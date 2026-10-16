"""Control the installed Unity MCP server using only Python's standard library.

The Unity editor implements performance_demo; analysis remains the same PowerShell
CLI used for offline captures. No OpenAI API key or Python MCP SDK is required.
"""
import argparse
import json
import sys
import time
import urllib.error
import urllib.request


class UnityMcpClient:
    def __init__(self, endpoint="http://127.0.0.1:8080/mcp", timeout=30):
        self.endpoint = endpoint
        self.timeout = timeout
        self.session = None
        self.sequence = 0
        self.protocol = "2025-03-26"
        self.rpc("initialize", {
            "protocolVersion": self.protocol, "capabilities": {},
            "clientInfo": {"name": "performance-demo-cli", "version": "1.0"},
        })
        self.rpc("notifications/initialized", {}, notification=True)

    def rpc(self, method, params, notification=False):
        self.sequence += 1
        request_id = self.sequence
        message = {"jsonrpc": "2.0", "method": method, "params": params}
        if not notification:
            message["id"] = request_id
        headers = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
        if self.session:
            headers["Mcp-Session-Id"] = self.session
            headers["MCP-Protocol-Version"] = self.protocol
        request = urllib.request.Request(self.endpoint, json.dumps(message).encode("utf-8"), headers)
        with urllib.request.urlopen(request, timeout=self.timeout) as response:
            self.session = response.headers.get("mcp-session-id", self.session)
            body = response.read().decode("utf-8")
        if notification and not body.strip():
            return None
        if body.lstrip().startswith("{"):
            messages = [json.loads(body)]
        else:
            messages = []
            for event in body.replace("\r\n", "\n").split("\n\n"):
                data = "\n".join(line[5:].lstrip() for line in event.splitlines() if line.startswith("data:"))
                if data:
                    messages.append(json.loads(data))
        for reply in messages:
            if reply.get("id") != request_id:
                continue
            if "error" in reply:
                raise RuntimeError(json.dumps(reply["error"], ensure_ascii=False))
            result = reply.get("result")
            if method == "initialize" and isinstance(result, dict):
                self.protocol = result.get("protocolVersion", self.protocol)
            return result
        if notification:
            return None
        raise RuntimeError("MCP response did not contain the requested JSON-RPC result.")

    def resource(self, uri):
        response = self.rpc("resources/read", {"uri": uri})
        values = [json.loads(item["text"]) for item in response.get("contents", []) if "text" in item]
        return values[0] if len(values) == 1 else values

    def tool(self, name, arguments):
        response = self.rpc("tools/call", {"name": name, "arguments": arguments})
        if response.get("isError"):
            raise RuntimeError(json.dumps(response, ensure_ascii=False))
        if "structuredContent" in response:
            payload = response["structuredContent"]
        else:
            blocks = [item["text"] for item in response.get("content", []) if item.get("type") == "text"]
            if not blocks:
                raise RuntimeError("MCP tool returned no JSON content.")
            payload = json.loads(blocks[0])
        while isinstance(payload, dict) and "success" in payload:
            if payload["success"] is False:
                raise RuntimeError(str(payload.get("error", payload)))
            if "data" not in payload or payload["data"] is None:
                break
            payload = payload["data"]
        return payload

    def performance(self, parameters):
        # Only retry read-only polling: a mutating request might already have
        # reached Unity when its connection drops during a domain reload.
        deadline = time.monotonic() + 20
        while True:
            try:
                return self.tool("execute_custom_tool", {"tool_name": "performance_demo", "parameters": parameters})
            except (RuntimeError, OSError) as error:
                if parameters.get("action") != "status" or time.monotonic() >= deadline:
                    raise
                time.sleep(.5)

    def close(self):
        if not self.session:
            return
        request = urllib.request.Request(self.endpoint, method="DELETE", headers={
            "Mcp-Session-Id": self.session, "MCP-Protocol-Version": self.protocol,
        })
        try:
            with urllib.request.urlopen(request, timeout=3):
                pass
        except OSError:
            pass
        self.session = None


def main():
    parser = argparse.ArgumentParser(description="Run Unity PerformanceDemo via MCP and optionally analyze its Profiler captures.")
    parser.add_argument("action", choices=["discover", "start", "status", "stop", "analyze"], nargs="?", default="start")
    parser.add_argument("--endpoint", default="http://127.0.0.1:8080/mcp")
    parser.add_argument("--instance", help="Exact Name@hash when multiple Unity instances are connected")
    parser.add_argument("--scenario", default="S1_Areas_20")
    parser.add_argument("--repetitions", type=int, default=0, help="0 keeps authored settings")
    parser.add_argument("--run-directory", help="Existing result directory for action=analyze")
    parser.add_argument("--no-analyze", action="store_true")
    parser.add_argument("--no-capture", action="store_true", help="Functional validation only; requires --no-analyze")
    parser.add_argument("--wait", action="store_true", help="Wait for a terminal job state")
    parser.add_argument("--timeout", type=float, default=1800)
    args = parser.parse_args()
    if args.no_capture and not args.no_analyze:
        parser.error("--no-capture requires --no-analyze")
    client = UnityMcpClient(args.endpoint)
    try:
        return execute(args, client)
    finally:
        client.close()


def execute(args, client):
    # Read server-specified resources before changing engine state.
    instances = client.resource("mcpforunity://instances")
    # Entering/leaving Play mode reloads the domain and briefly disconnects the
    # editor websocket. Keep the same MCP session and wait for registration.
    reconnect_deadline = time.monotonic() + 20
    while instances.get("instance_count") == 0 and time.monotonic() < reconnect_deadline:
        time.sleep(.5)
        instances = client.resource("mcpforunity://instances")
    if args.instance:
        client.tool("set_active_instance", {"instance": args.instance})
    elif instances.get("instance_count") != 1:
        raise RuntimeError("Expected one Unity instance. Use discover with --instance Name@hash to select a target. Instances: " + json.dumps(instances))
    custom = client.resource("mcpforunity://custom-tools")
    state = client.resource("mcpforunity://editor/state")
    if args.action == "discover":
        tools = client.rpc("tools/list", {})
        custom_summary = custom.get("data", {})
        print(json.dumps({"instances": instances, "customTools": {
            "project_id": custom_summary.get("project_id"), "tool_count": custom_summary.get("tool_count"),
            "performance_demo": [tool for tool in custom_summary.get("tools", []) if tool.get("name") == "performance_demo"],
        }, "editor": state,
            "routingTools": [tool for tool in tools.get("tools", []) if tool["name"] in ("execute_custom_tool", "set_active_instance")]}, ensure_ascii=False, indent=2))
        return 0
    parameters = {"action": args.action}
    if args.action == "start":
        parameters.update(scenario=args.scenario, repetitions=args.repetitions, analyze=not args.no_analyze, capture_profiler=not args.no_capture)
    if args.run_directory:
        parameters["run_directory"] = args.run_directory
    job = client.performance(parameters)
    print(json.dumps(job, ensure_ascii=False, indent=2), flush=True)
    if not args.wait or args.action not in ("start", "stop", "analyze"):
        return 0
    deadline = time.monotonic() + args.timeout
    previous = None
    while job.get("phase") not in ("Completed", "Failed", "Stopped"):
        if time.monotonic() > deadline:
            raise TimeoutError("Wait timed out; the Unity job is still queryable with status.")
        time.sleep(2)
        job = client.performance({"action": "status"})
        current = (job.get("phase"), job.get("runnerPhase"))
        if current != previous:
            print(json.dumps({"phase": current[0], "runnerPhase": current[1]}, ensure_ascii=False), flush=True)
            previous = current
    print(json.dumps(job, ensure_ascii=False, indent=2), flush=True)
    return 0 if job["phase"] == "Completed" else 2


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (RuntimeError, ValueError, OSError, TimeoutError, urllib.error.URLError) as error:
        print(str(error), file=sys.stderr)
        sys.exit(1)

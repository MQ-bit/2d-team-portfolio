using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

// Run from the repository root:
// dotnet run --project Assets/02.Scripts/Potion/Tests~/BalanceRuntime/BalanceRuntime.csproj
internal static class Program
{
    private static int _checks;
    private static readonly Dictionary<int, PotionCombinationData> Combinations = new();
    private static PotionVaporizationSettings Settings;

    private static void Main()
    {
        Settings = Load<PotionVaporizationSettings>("Assets/07.Datas/PotionData/PotionVaporizationSettings.asset");
        AssetChecks();
        ConversionChecks();
        BatchChecks();
        LifeStealChecks();
        ControlChecks();
        TimelineChecks();
        CombinationEntryChecks();
        Console.WriteLine($"PASS: {_checks} balance checks (production source; Unity lifecycle/physics/rendering excluded).");
    }

    private static void AssetChecks()
    {
        var expected = new Dictionary<int, (float interval, float[] damage, bool frost, float paralysis, bool burn, bool poison)>
        {
            [1]=(0.5f,new[]{30f},false,0,true,false),
            [2]=(0.75f,new[]{26f},false,0,false,true),
            [3]=(0.5f,new[]{18f},true,0,false,false),
            [4]=(0.5f,new[]{16f},false,0.15f,false,false),
            [6]=(0.5f,new[]{32f,22f},false,0,true,true),
            [7]=(0.5f,new[]{24f},true,0,false,false),
            [8]=(0.5f,new[]{30f,20f},false,0.15f,true,false),
            [10]=(0.75f,new[]{32f},true,0,false,true),
            [11]=(0.5f,new[]{30f,12f},false,0.2f,false,true),
            [13]=(0.5f,new[]{16f,16f},true,0.15f,false,false)
        };
        for (int id=1; id<=15; id++)
        {
            string path = Directory.GetFiles("Assets/07.Datas/PotionCombinationData", $"{id:D2}_*.asset").Single();
            PotionCombinationData data = Load<PotionCombinationData>(path);
            Combinations.Add(id,data);
            Equal((float)data.Combination,id,"combination identity");
            Equal(data.EffectRadius,4,"radius field");
            Equal(data.SpreadDuration,0.4f,"spread duration");
            Check(data.InheritThrownEffectsFrom==PotionElement.None,"explicit combination profile");
            Equal(data.LifeStealRatio,id<=4?0.15f:0,"life steal ratio");
            Check(!data.Effects.Any(e=>e.Operation==PotionEffectOperation.ApplyStatus && e.Status==PotionStatusEffectTracker.Slowed),"no poison slow");
            if (!expected.TryGetValue(id,out var spec))
            {
                Check(!data.CreatesFloor && !data.ApplyOnEnter,"instant combination has no floor");
                Equal(data.TickInterval,0,"instant combination has no ticks");
                Check(data.Effects.Count==0,"vaporization uses runtime output, not stale fixed damage");
                continue;
            }
            Check(data.CreatesFloor && data.ApplyOnEnter,"persistent combination applies on entry");
            Equal(data.FloorDuration,6,"floor duration");
            Equal(data.TickInterval,spec.interval,"floor interval");
            var tick=data.GetEffects(PotionEffectPhase.FloorTick).ToArray();
            Check(tick.Where(e=>e.Operation==PotionEffectOperation.Damage).Select(e=>e.Amount).SequenceEqual(spec.damage),"floor damage amounts");
            Check(tick.Count(e=>e.Operation==PotionEffectOperation.AddFrostStacks)==(spec.frost?1:0),"frost stacks per pulse");
            Equal(tick.Where(e=>e.Operation==PotionEffectOperation.ApplyStatus).Select(e=>e.Duration).SingleOrDefault(),spec.paralysis,"paralysis duration");
            var exit=data.GetEffects(PotionEffectPhase.FloorExit).ToArray();
            Check(exit.Length==(spec.burn?1:0)+(spec.poison?1:0),"residual count");
            foreach(var effect in exit)
            {
                bool burn=effect.Status==PotionStatusEffectTracker.Burning;
                Equal(effect.Amount,burn?8:24,"residual amount");
                Equal(effect.TickInterval,burn?1:0.75f,"residual interval");
                Equal(effect.Duration,burn?2:4.5f,"residual duration");
                Check(!effect.ApplyImmediately,"residual first pulse delayed");
            }
            Equal(data.Frost.FreezeDuration,2,"combination freeze duration");
            Check(data.Frost.FreezeThreshold==3 && data.Frost.MoveSpeedMultipliers.SequenceEqual(new[]{0.85f,0.7f}),"frost threshold and slows");
        }
        Equal(Combinations[7].GetEffects(PotionEffectPhase.Impact).Single().Amount,72,"thermal shock formation damage");
    }

    private static void ConversionChecks()
    {
        string[] names={"FirePotion","PoisonPotion","FrostPotion","LightningPotion","LifePotion"};
        float[] amounts={172.8f,120,96,90,90};
        for(int i=0;i<names.Length;i++)
        {
            var data=Load<PotionData>($"Assets/07.Datas/PotionData/{names[i]}.asset");
            var effects=PotionAreaEffectTracker.GetVaporizationEffects(data,6,Settings);
            Equal(effects.Sum(e=>e.Amount),amounts[i],"full remaining base output");
            Equal(PotionAreaEffectTracker.GetVaporizationEffects(data,2.25f,Settings).Sum(e=>e.Amount),amounts[i]*2.25f/6,"fractional remaining ticks");
            Check(PotionAreaEffectTracker.GetVaporizationEffects(data,0,Settings).Count==0,"expired floor has no output");
            Equal(PotionAreaEffectTracker.GetVaporizationEffects(data,6,null).Sum(e=>e.Amount),amounts[i],"fallback settings match asset");
        }
        var thermal=PotionAreaEffectTracker.GetVaporizationEffects(Combinations[7],6,Settings);
        Check(thermal.Count==1 && thermal[0].Element==PotionElement.Ice,"thermal formation damage is not converted");
        Equal(thermal[0].Amount,576,"thermal frost output");
        var toxic=PotionAreaEffectTracker.GetVaporizationEffects(Combinations[6],6,Settings);
        Equal(toxic.Single(e=>e.Element==PotionElement.Fire).Amount,307.2f,"combined fire multiplier");
        Equal(toxic.Single(e=>e.Element==PotionElement.Poison).Amount,264,"combined poison multiplier");
        Check(toxic.All(e=>e.Operation==PotionEffectOperation.Damage),"residuals never converted");
    }

    private static void BatchChecks()
    {
        Time.timeAsDouble=0;
        var health=new Health(1000);
        var enemy=Receiver(health,PotionTargetKind.Enemy);
        var batch=new PotionVaporizationBatch(Settings);
        Queue(batch,enemy,Combinations[6],6);
        Queue(batch,enemy,Combinations[8],6);
        float fire=0,poison=0,lightning=0;
        enemy.DamageApplied+=result=>
        {
            if(result.Element==PotionElement.Fire) fire+=result.ActualDamage;
            if(result.Element==PotionElement.Poison) poison+=result.ActualDamage;
            if(result.Element==PotionElement.Lightning) lightning+=result.ActualDamage;
        };
        batch.Apply();
        Equal(1000-health.CurrentHealth,240,"one bottle shares damage cap across surfaces");
        float rawFire=307.2f+288, rawPoison=264, rawLightning=360;
        float scale=240/(rawFire+rawPoison+rawLightning);
        Equal(fire,rawFire*scale,"proportional fire share");
        Equal(poison,rawPoison*scale,"proportional poison share");
        Equal(lightning,rawLightning*scale,"proportional lightning share");
        Equal(enemy.GetStatusRemainingTime(PotionStatusEffectTracker.Paralyzed),0.6f,"vaporization paralysis");

        var ownerHealth=new Health(1000);
        var owner=Receiver(ownerHealth,PotionTargetKind.Player);
        batch=new PotionVaporizationBatch(Settings);
        Queue(batch,owner,Combinations[6],6,ownerHealth);
        Queue(batch,owner,Combinations[8],6,ownerHealth);
        batch.Apply();
        Equal(1000-ownerHealth.CurrentHealth,60,"self damage is 25% after raw 240 cap");

        ownerHealth=new Health(1000);
        owner=Receiver(ownerHealth,PotionTargetKind.Player);
        var victimHealth=new Health(1000);
        var victim=Receiver(victimHealth,PotionTargetKind.Enemy);
        var life=Load<PotionData>("Assets/07.Datas/PotionData/LifePotion.asset");
        batch=new PotionVaporizationBatch(Settings);
        Queue(batch,owner,life,6);
        Queue(batch,owner,life,6);
        Queue(batch,victim,Combinations[1],6,ownerHealth);
        batch.Apply();
        Equal(victimHealth.CurrentHealth,760,"vampiric vapor damage capped");
        Equal(ownerHealth.TotalHealing,120,"direct healing and life steal share recipient cap");

        var a=Receiver(new Health(1000),PotionTargetKind.Enemy);
        var b=Receiver(new Health(1000),PotionTargetKind.Enemy);
        batch=new PotionVaporizationBatch(Settings);
        Queue(batch,a,Combinations[10],6);
        Queue(batch,b,Combinations[13],6);
        batch.Apply();
        Equal(a.GetStatusRemainingTime(PotionStatusEffectTracker.Frozen),2.5f,"poison-only damage with frost stacks still vapor-freezes");
        Equal(b.GetStatusRemainingTime(PotionStatusEffectTracker.Frozen),2.5f,"ice/lightning vapor-freezes");
        Equal(b.GetStatusRemainingTime(PotionStatusEffectTracker.Paralyzed),0.6f,"ice/lightning vapor-paralyzes");
        Equal(a.Damageable.CurrentHealth,760,"each recipient has own cap");
        Equal(b.Damageable.CurrentHealth,760,"second recipient has own cap");
    }

    private static void LifeStealChecks()
    {
        Time.timeAsDouble=0;
        var owner=new Health(1000);
        var enemy=Receiver(new Health(10),PotionTargetKind.Enemy);
        var source=new PotionEffectSource(123,owner:owner,lifeStealRatio:0.15f);
        enemy.ApplyEffect(PotionEffectDefinition.CreateInstant(PotionEffectOperation.Damage,PotionElement.Fire,30),Combinations[1],source);
        Equal(owner.TotalHealing,1.5f,"overkill heals only actual HP removed");

        var self=Receiver(owner,PotionTargetKind.Player);
        self.ApplyEffect(PotionEffectDefinition.CreateInstant(PotionEffectOperation.Damage,PotionElement.Fire,30),Combinations[1],source);
        Equal(owner.TotalHealing,1.5f,"self damage grants no life steal");
        var ally=Receiver(new Health(100),PotionTargetKind.Player);
        ally.ApplyEffect(PotionEffectDefinition.CreateInstant(PotionEffectOperation.Damage,PotionElement.Fire,30),Combinations[1],source);
        Equal(owner.TotalHealing,1.5f,"only enemy damage grants life steal");

        owner=new Health(1000);
        foreach(int id in new[]{1,2})
        {
            enemy=Receiver(new Health(1000),PotionTargetKind.Enemy);
            source=new PotionEffectSource(id,owner:owner,lifeStealRatio:0.15f);
            var residual=Combinations[id].GetEffects(PotionEffectPhase.FloorExit).Single();
            enemy.ApplyEffect(residual,Combinations[id],source);
            Time.timeAsDouble+=residual.TickInterval;
            enemy.GetType().GetMethod("TickStatuses",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(enemy,null);
        }
        Equal(owner.TotalHealing,(8+24)*0.15f,"burning and poisoned residual ticks grant life steal");

        Time.timeAsDouble=0;
        owner=new Health(1000);
        var victim=Receiver(new Health(120),PotionTargetKind.Enemy);
        source=new PotionEffectSource(99,owner:owner,lifeStealRatio:0.15f);
        for(int i=0;i<4;i++) victim.ApplyEffect(PotionEffectDefinition.CreateInstant(PotionEffectOperation.Damage,PotionElement.Fire,30),Combinations[1],source);
        Equal(owner.TotalHealing,18,"120 enemy HP grants at most 18 healing");
    }

    private static void ControlChecks()
    {
        double now=0;
        var state=new PotionStatusEffectState(()=>now);
        var source=new PotionEffectSource(1);
        state.AddFrostStacks(Combinations[3].Frost,source,1,0.5f);
        Equal(state.MoveSpeedMultiplier,0.85f,"first frost slow");
        now=0.5;
        state.AddFrostStacks(Combinations[3].Frost,source,1,0.5f);
        Equal(state.MoveSpeedMultiplier,0.7f,"second frost slow");
        now=1;
        state.AddFrostStacks(Combinations[3].Frost,source,1,0.5f);
        Check(state.FrostStackCount==0 && !state.CanMove,"freeze clears frost stacks");
        now=1.5;
        state.AddFrostStacks(Combinations[3].Frost,source,1,0.5f);
        Check(state.FrostStackCount==0,"frozen targets cannot accumulate frost");
        state.Apply(new PotionStatusEffectInfo(PotionStatusEffectTracker.Frozen,0.1f,source));
        Equal(state.GetRemainingTime(PotionStatusEffectTracker.Frozen),1.5f,"shorter freeze cannot truncate existing freeze");
        now=3;
        state.AddFrostStacks(Combinations[3].Frost,source,1,0.5f);
        Check(state.FrostStackCount==1,"frost stacking resumes after thaw");
        state.SetAreaSlow(2,0.8f);
        Equal(state.MoveSpeedMultiplier,0.8f,"slows choose strongest rather than multiply");
        state.Apply(new PotionStatusEffectInfo(PotionStatusEffectTracker.Paralyzed,0.6f,source));
        state.Apply(new PotionStatusEffectInfo(PotionStatusEffectTracker.Paralyzed,0.15f,source));
        Equal(state.GetRemainingTime(PotionStatusEffectTracker.Paralyzed),0.6f,"shorter paralysis cannot truncate existing paralysis");
        state.Apply(new PotionStatusEffectInfo(PotionStatusEffectTracker.Paralyzed,0.6f,source));
        Equal(state.GetRemainingTime(PotionStatusEffectTracker.Paralyzed),0.6f,"control duplicates do not add duration");

        state=new PotionStatusEffectState(()=>now);
        state.Apply(new PotionStatusEffectInfo(PotionStatusEffectTracker.Burning,2,source,PotionElement.Fire,8,1));
        state.Apply(new PotionStatusEffectInfo(PotionStatusEffectTracker.Poisoned,4.5f,new PotionEffectSource(2),PotionElement.Poison,24,0.75f));
        state.RemoveSource(1);
        Check(!state.IsActive(PotionStatusEffectTracker.Burning) && state.IsActive(PotionStatusEffectTracker.Poisoned),"source cleanup preserves other floors' residuals");
    }

    private static void TimelineChecks()
    {
        int pulses=0;
        var timeline=new PotionAreaTimeline<int>(6,0.5,true,_=>true,_=>{},_=>pulses++,(_,_)=>{});
        timeline.Synchronize(new[]{1},0);
        Check(pulses==1,"entry pulse immediate");
        timeline.Synchronize(new[]{1},0.5);
        Check(pulses==2,"next pulse follows interval");
        timeline.Synchronize(new[]{1},6);
        Check(pulses==2,"expired floor cannot pulse");
    }

    private static void CombinationEntryChecks()
    {
        var tracker=new PotionAreaEffectTracker();
        var original=Load<PotionData>("Assets/07.Datas/PotionData/FrostPotion.asset");
        tracker.Enter(original,new PotionEffectSource(1,potion:original));
        float baseDamage=0,combinationDamage=0;
        tracker.Pulse(1,0,(effect,_,_)=>{if(effect.Operation==PotionEffectOperation.Damage)baseDamage+=effect.Amount;});
        Equal(baseDamage,6,"original floor entry pulse");
        tracker.Enter(Combinations[7],new PotionEffectSource(2,combination:Combinations[7]));
        tracker.Pulse(2,0,(effect,_,_)=>{if(effect.Operation==PotionEffectOperation.Damage)combinationDamage+=effect.Amount;});
        Equal(combinationDamage,24,"reaction entry ignores partner base-floor cooldown");
        tracker.Pulse(2,0,(effect,_,_)=>{if(effect.Operation==PotionEffectOperation.Damage)combinationDamage+=effect.Amount;});
        Equal(combinationDamage,24,"repeated combination pulse in same frame is suppressed");
    }

    private static void Queue(PotionVaporizationBatch batch, PotionEffectReceiver target,
        IPotionEffectData profile,float remaining,IDamageable owner=null)
    {
        var ownership=new PotionAreaOwnership();
        ownership.SetOwner(owner);
        var source=new PotionEffectSource(42,owner:owner,lifeStealRatio:profile.LifeStealRatio,
            areaOwnership:ownership,vaporizationBatch:batch);
        batch.Queue(target,profile,source,PotionAreaEffectTracker.GetVaporizationEffects(profile,remaining,Settings));
    }

    private static PotionEffectReceiver Receiver(Health health,PotionTargetKind kind)
    {
        var receiver=new PotionEffectReceiver();
        Set(receiver,"<Damageable>k__BackingField",health);
        Set(receiver,"_targetKind",kind);
        return receiver;
    }

    private static T Load<T>(string path) where T:new()
    {
        string yaml=File.ReadAllText(path);
        var value=new T();
        foreach(Match match in Regex.Matches(yaml,@"^  (_\w+): ([\d.]+)\r?$",RegexOptions.Multiline))
            Assign(value,match.Groups[1].Value,match.Groups[2].Value);
        if(value is IPotionEffectData)
        {
            string effectYaml=Regex.Match(yaml,@"  _effects:(.*?)  _frost:",RegexOptions.Singleline).Groups[1].Value;
            var effects=new List<PotionEffectDefinition>();
            foreach(string block in Regex.Split(effectYaml,@"(?=  - _phase:)").Skip(1))
            {
                var effect=new PotionEffectDefinition();
                foreach(Match match in Regex.Matches(block,@"(_\w+): ([\d.]+)"))
                    Assign(effect,match.Groups[1].Value,match.Groups[2].Value);
                effects.Add(effect);
            }
            Set(value,"_effects",effects);
            var frost=new PotionFrostSettings();
            string frostYaml=Regex.Match(yaml,@"  _frost:(.*?)(?=\r?\n  _\w+:)",RegexOptions.Singleline).Groups[1].Value;
            foreach(Match match in Regex.Matches(frostYaml,@"(_\w+): ([\d.]+)"))
                Assign(frost,match.Groups[1].Value,match.Groups[2].Value);
            var multipliers=Regex.Matches(frostYaml,@"- ([\d.]+)").Select(m=>float.Parse(m.Groups[1].Value,CultureInfo.InvariantCulture)).ToList();
            Set(frost,"_moveSpeedMultipliers",multipliers);
            Set(value,"_frost",frost);
        }
        return value;
    }

    private static void Assign(object value,string name,string raw)
    {
        FieldInfo field=value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);
        if(field==null)return;
        object parsed=field.FieldType==typeof(bool)?raw=="1":field.FieldType.IsEnum?
            Enum.ToObject(field.FieldType,int.Parse(raw,CultureInfo.InvariantCulture)):
            Convert.ChangeType(raw,field.FieldType,CultureInfo.InvariantCulture);
        field.SetValue(value,parsed);
    }
    private static void Set(object value,string name,object fieldValue) =>
        value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(value,fieldValue);
    private static void Check(bool condition,string message)
    {
        _checks++;
        if(!condition)throw new Exception(message);
    }
    private static void Equal(float actual,float expected,string message) =>
        Check(Math.Abs(actual-expected)<0.002f,$"{message}: expected {expected}, got {actual}");
    private sealed class Health:IDamageable
    {
        public float CurrentHealth {get;private set;}
        public float TotalHealing {get;private set;}
        public bool IsDead=>CurrentHealth<=0;
        public event Action<float,float> OnHealthChanged;
        public event Action OnDeath;
        internal Health(float health){CurrentHealth=health;}
        public void RestoreHealth(float amount){TotalHealing+=amount;CurrentHealth+=amount;}
        public void DecreaseHealth(float amount){CurrentHealth=Math.Max(0,CurrentHealth-amount);}
    }
}

using RimWorld;
using Verse;
using Verse.AI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System.Linq;
using UnityEngine;

namespace MendAndRecycle
{
    [StaticConstructorOnStartup]
    public class WorkGiver_DoBill : RimWorld.WorkGiver_DoBill
    {
	public static bool inJobOnThing = false;
	public static Pawn workerPawn;
	public static Type wgdb;

	private static List<Thing> relevantThings => (List<Thing>) wgdb.GetField("relevantThings", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

	private static HashSet<Thing> processedThings => (HashSet<Thing>) wgdb.GetField("processedThings", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
	
	private static List<Thing> newRelevantThings => (List<Thing>) wgdb.GetField("newRelevantThings", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

	private static List<IngredientCount> ingredientsOrdered => (List<IngredientCount>) wgdb.GetField("ingredientsOrdered", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

	private static List<Thing> tmpMedicine => (List<Thing>) wgdb.GetField("tmpMedicine", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

	private static ThingDef mendingTable; 

	static WorkGiver_DoBill() {
	    var harmony = new Harmony("com.lp-programming.mending");
	    wgdb = typeof(RimWorld.WorkGiver_DoBill);
	    harmony.Patch(wgdb.GetMethod("TryFindBestBillIngredientsInSet_NoMix", BindingFlags.Static | BindingFlags.NonPublic),
			  new HarmonyMethod(typeof(WorkGiver_DoBill).GetMethod("TryFindBestBillIngredientsInSet_NoMix", BindingFlags.Static | BindingFlags.Public)),
			  null);
	    mendingTable = ThingDef.Named("TableMending");
	}


	public static bool TryFindBestBillIngredientsInSet_NoMix(List<Thing> availableThings, Bill bill, List<ThingCount> chosen, IntVec3 rootCell, bool alreadySorted, out bool __result) {
	    __result = false;
	    if (!inJobOnThing) {
		return true;
	    }
	    if (!alreadySorted) {
		Comparison<Thing> comparison = delegate(Thing t1, Thing t2)		{
		    float num5 = (float)(t1.Position - rootCell).LengthHorizontalSquared;
		    float value = (float)(t2.Position - rootCell).LengthHorizontalSquared;
		    return num5.CompareTo(value);
		};
		availableThings.Sort(comparison);
	    }
	    RecipeDef recipe = bill.recipe;
	    chosen.Clear();
	    WorkGiver_DoBill.availableCounts.Clear();
	    WorkGiver_DoBill.availableCounts.GenerateFrom(availableThings);
	    for (int i = 0; i < WorkGiver_DoBill.ingredientsOrdered.Count; i++)
	    {
		IngredientCount ingredientCount = recipe.ingredients[i];
		bool flag = false;
		for (int j = 0; j < WorkGiver_DoBill.availableCounts.Count; j++)
		{
		    float num = (float)ingredientCount.CountRequiredOfFor(WorkGiver_DoBill.availableCounts.GetDef(j), bill.recipe);
		    if ((recipe.ignoreIngredientCountTakeEntireStacks || num <= WorkGiver_DoBill.availableCounts.GetCount(j)) && ingredientCount.filter.Allows(WorkGiver_DoBill.availableCounts.GetDef(j)) && (ingredientCount.IsFixedIngredient || bill.ingredientFilter.Allows(WorkGiver_DoBill.availableCounts.GetDef(j))))
		    {
			for (int k = 0; k < availableThings.Count; k++)
			{
			    if (availableThings[k].def == WorkGiver_DoBill.availableCounts.GetDef(j))
			    {
				int num2 = availableThings[k].stackCount - ThingCountUtility.CountOf(chosen, availableThings[k]);
				if (num2 > 0)
				{
				    int num3 = Mathf.Min(Mathf.FloorToInt(num), num2);
				    Thing extra = WorkGiver_Utils.CanFindSecondaryItems(workerPawn, rootCell, availableThings[k], bill);
				    if (extra == null) {
					continue;
				    }

				    ThingCountUtility.AddToList(chosen, availableThings[k], num3);
				    ThingCountUtility.AddToList(chosen, extra, extra.stackCount);
				    num -= (float)num3;
				    if (num < 0.001f)
				    {
					flag = true;
					float num4 = WorkGiver_DoBill.availableCounts.GetCount(j);
					num4 -= (float)ingredientCount.CountRequiredOfFor(WorkGiver_DoBill.availableCounts.GetDef(j), bill.recipe);
					WorkGiver_DoBill.availableCounts.SetCount(j, num4);
					
					break;
				    }
				}
			    }
			}
			if (flag)
			{
			    break;
			}
		    }
		}
		if (!flag)
		{
		    return false;
		}
	    }
	    __result = true;
	    return false;
	}
	
	
        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
	    if (thing.def != mendingTable) {
		return base.JobOnThing(pawn, thing, forced);
	    }
	    inJobOnThing = true;
	    workerPawn = pawn;
            var job = base.JobOnThing(pawn, thing, forced);
	    inJobOnThing = false;
            if (job != null && job.def == JobDefOf.DoBill && job.RecipeDef.Worker is RecipeWorkerWithJob worker)
            {
                job = new Job(worker.Job, job.targetA)
                {
                    targetQueueB = job.targetQueueB,
                    countQueue = job.countQueue,
                    haulMode = job.haulMode,
                    bill = job.bill
                };
		return WorkGiver_Utils.GetSecondaryItemsForJob(worker, pawn, job);
            }


            return job;
        }




	private static WorkGiver_DoBill.DefCountList availableCounts = new WorkGiver_DoBill.DefCountList();

	// Token: 0x02001992 RID: 6546
	private class DefCountList
	{
	    // Token: 0x1700171D RID: 5917
	    // (get) Token: 0x06009454 RID: 37972 RVA: 0x002F6984 File Offset: 0x002F4B84
	    public int Count
	    {
		get
		{
		    return this.defs.Count;
		}
	    }

	    // Token: 0x1700171E RID: 5918
	    public float this[ThingDef def]
	    {
		get
		{
		    int num = this.defs.IndexOf(def);
		    if (num < 0)
		    {
			return 0f;
		    }
		    return this.counts[num];
		}
		set
		{
		    int num = this.defs.IndexOf(def);
		    if (num < 0)
		    {
			this.defs.Add(def);
			this.counts.Add(value);
			num = this.defs.Count - 1;
		    }
		    else
		    {
			this.counts[num] = value;
		    }
		    this.CheckRemove(num);
		}
	    }

	    // Token: 0x06009457 RID: 37975 RVA: 0x002F6A1E File Offset: 0x002F4C1E
	    public float GetCount(int index)
	    {
		return this.counts[index];
	    }

	    // Token: 0x06009458 RID: 37976 RVA: 0x002F6A2C File Offset: 0x002F4C2C
	    public void SetCount(int index, float val)
	    {
		this.counts[index] = val;
		this.CheckRemove(index);
	    }

	    // Token: 0x06009459 RID: 37977 RVA: 0x002F6A42 File Offset: 0x002F4C42
	    public ThingDef GetDef(int index)
	    {
		return this.defs[index];
	    }

	    // Token: 0x0600945A RID: 37978 RVA: 0x002F6A50 File Offset: 0x002F4C50
	    private void CheckRemove(int index)
	    {
		if (this.counts[index] == 0f)
		{
		    this.counts.RemoveAt(index);
		    this.defs.RemoveAt(index);
		}
	    }

	    // Token: 0x0600945B RID: 37979 RVA: 0x002F6A7D File Offset: 0x002F4C7D
	    public void Clear()
	    {
		this.defs.Clear();
		this.counts.Clear();
	    }

	    // Token: 0x0600945C RID: 37980 RVA: 0x002F6A98 File Offset: 0x002F4C98
	    public void GenerateFrom(List<Thing> things)
	    {
		this.Clear();
		for (int i = 0; i < things.Count; i++)
		{
		    ThingDef def = things[i].def;
		    this[def] += (float)things[i].stackCount;
		}
	    }

	    // Token: 0x040061BE RID: 25022
	    private List<ThingDef> defs = new List<ThingDef>();

	    // Token: 0x040061BF RID: 25023
	    private List<float> counts = new List<float>();
	}

	
    }
}

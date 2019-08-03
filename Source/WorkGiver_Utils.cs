using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using System;

namespace MendAndRecycle {
    public static class WorkGiver_Utils {
	public static Job GetSecondaryItemsForJob(RecipeWorkerWithJob worker, Pawn pawn, Job job) {
	    Thing target = job.targetQueueB[0].Thing;
//	    Thing target = job.targetB.Thing;
//	    return;


            List<ThingDefCountClass> costListAdj = target.CostListAdjusted ();

            List<ThingDefCountClass> thingCountList;

            if (!costListAdj.NullOrEmpty ()) {
                thingCountList = costListAdj;
            } else if (!target.def.smeltProducts.NullOrEmpty ()) {
                thingCountList = target.def.smeltProducts;
            } else {
                thingCountList = null;
            }

	    Predicate<Thing> search = t => !t.IsForbidden(pawn) && pawn.CanReserve(t);
	    
	    if (!thingCountList.NullOrEmpty()) {
		Thing closesthing = GenClosest.ClosestThingReachable(
				        pawn.Position,
					pawn.Map,
					ThingRequest.ForDef(thingCountList[0].thingDef),
					PathEndMode.ClosestTouch,
					TraverseParms.For(pawn, Danger.None, TraverseMode.ByPawn),
					job.bill.ingredientSearchRadius,
					search);
		if (closesthing!=null) {
		    return new Job(worker.Job, job.targetA) {
			targetQueueB = new List<LocalTargetInfo> {
			    closesthing,
			    target,
			    closesthing
			},
			countQueue = new List<int>{
			    closesthing.stackCount,
			    1,
			    1
			},
			haulMode = job.haulMode,
			bill = job.bill,
			targetC = closesthing
		    };
		}
		return null;
		
	    }
	    return job;
	}
    }
}

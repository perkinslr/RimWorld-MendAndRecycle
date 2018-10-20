﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using RimWorld;
using Verse;

namespace MendAndRecycle
{
    class MendAndRecycleMod : Mod
    {
        static readonly FieldInfo thingFilterallowedDefsField = typeof(ThingFilter).GetField("allowedDefs", BindingFlags.NonPublic | BindingFlags.Instance);
        
        public MendAndRecycleMod(ModContentPack mcp) : base(mcp)
        {
            base.GetSettings<Settings>();

            LongEventHandler.ExecuteWhenFinished(Inject);
        }

        public override string SettingsCategory()
        {
            return ResourceBank.MendAndRecycle;
        }

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            Settings.DoSettingsWindowContents(inRect);
        }

        static void Inject()
        {
            if (!Settings.requiresFuel) {
                RemoveFuel();
            }
			if (!Settings.requiresPower)
			{
				RemovePower();
			}

            SortApparelsInComplexity();
        }

        static void RemoveFuel() {
            LocalDefOf.Recipe.MakeMendingKit.recipeUsers.Clear();
            LocalDefOf.Thing.TableMending.comps.RemoveAll(p => p.GetType() == typeof(CompProperties_Refuelable));
        }
		static void RemovePower()
		{
			LocalDefOf.Thing.TableMending.comps.RemoveAll(p => p.GetType() == typeof(CompProperties_Power));
		}

        static void SortApparelsInComplexity() {
			// select and group ThingDefs by complexity
			var defs = (
				from def in DefDatabase<ThingDef>.AllDefs
				where (def.IsApparel)
				group def by HasComponents(def) into g
				select new { isComplex = g.Key, list = g.ToList() }
			);

			foreach (var def in defs)
			{
				RecipeDef recipe;
				if (def.isComplex)
				{
					recipe = LocalDefOf.Recipe.MendComplexApparel;
				}
				else
				{
					recipe = LocalDefOf.Recipe.MendSimpleApparel;
				}

				AddDefToFilters(recipe, def.list);
			}
        }

        static bool HasComponents(ThingDef def) {
            if (def.costList == null) {
                return false;
            }

			foreach (var tdcc in def.costList)
			{
				if (tdcc.thingDef == ThingDefOf.ComponentIndustrial ||
					tdcc.thingDef == ThingDefOf.ComponentSpacer)
				{
                    return true;
				}
			}

            return false;
        }

        static void AddDefToFilters(RecipeDef recipe, List<ThingDef> defs)
        {
            thingFilterallowedDefsField.SetValue(recipe.fixedIngredientFilter, new HashSet<ThingDef>(defs));
            thingFilterallowedDefsField.SetValue(recipe.defaultIngredientFilter, new HashSet<ThingDef>(defs));
        }
    }
}
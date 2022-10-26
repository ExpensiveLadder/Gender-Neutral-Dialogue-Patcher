using System;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;
using Noggog;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using System.Collections.Generic;

namespace GenderDialoguePatch
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "YourPatcher.esp")
                .Run(args);
        }

        public static readonly FormLink<Keyword> ActorNonBinary = FormKey.Factory("EBDA00:Update.esm").ToLink<Keyword>();
        public static readonly FormLink<Global> He = FormKey.Factory("000CCC:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> She = FormKey.Factory("000CCD:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> They = FormKey.Factory("000CCE:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> Brother = FormKey.Factory("003CC9:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> Sister = FormKey.Factory("003CCA:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> Sibling = FormKey.Factory("003CCB:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> Lad = FormKey.Factory("003CC6:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> Lass = FormKey.Factory("003CC7:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> LadLassBackup = FormKey.Factory("003CC8:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> Miss = FormKey.Factory("003CD0:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> Mister = FormKey.Factory("003CCF:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> MisterMissBackup = FormKey.Factory("003CD1:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> Man = FormKey.Factory("003CD8:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> Woman = FormKey.Factory("003CD9:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> ManWomanBackup = FormKey.Factory("003CDA:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> Father = FormKey.Factory("003CDD:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> Mother = FormKey.Factory("003CDC:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> Parent = FormKey.Factory("003CDE:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> Child = FormKey.Factory("003CF7:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> Daughter = FormKey.Factory("003CF6:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> Son = FormKey.Factory("003CF5:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> Lady = FormKey.Factory("003D18:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> Lord = FormKey.Factory("003D17:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> Mtheydy = FormKey.Factory("003B19:Gender-Neutral Dialogue.esp").ToLink<Global>();


        public static readonly List<FormLink<Global>> Pronouns = new()
        {
            He,
            She,
            They,
            Brother,
            Sister,
            Sibling,
            Lad,
            Lass,
            LadLassBackup,
            Miss,
            Mister,
            MisterMissBackup,
            Man,
            Woman,
            ManWomanBackup,
            Father,
            Mother,
            Parent,
            Child,
            Daughter,
            Son,
            Lady,
            Lord,
            Mtheydy
        };

        public static bool IsThing(IDialogResponsesGetter response)
        {
            var returnTrue = false;
            if (response.Conditions != null)
            {
                foreach (var condition in response.Conditions)
                {
                    if (condition.Data != null)
                    {
                        var conditionFunction = (FunctionConditionData)condition.Data.DeepCopy();
                        if (conditionFunction.Function == Condition.Function.HasKeyword)
                        {
                            if (conditionFunction.ParameterOneRecord.FormKey.GetHashCode() == ActorNonBinary.FormKey.GetHashCode()) return false;
                        }
                        /*
                        else if (conditionFunction.Function == Condition.Function.GetGlobalValue)
                        {
                            foreach (var pronoun in Pronouns)
                            {
                                if (conditionFunction.ParameterOneRecord.FormKey == pronoun.FormKey) return false;
                            }
                        }
                        */
                        else if (conditionFunction.Function == Condition.Function.GetPCIsSex)
                        {
                            returnTrue = true;
                        }
                        else if (conditionFunction.Function == Condition.Function.GetIsSex && conditionFunction.RunOnType != Condition.RunOnType.Subject)
                        {
                            returnTrue = true;
                        }
                    }
                }
            }
            return returnTrue;
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            //Your code here!
            foreach (var item in state.LoadOrder.PriorityOrder.DialogResponses().WinningContextOverrides(state.LinkCache))
            {
                if (IsThing(item.Record))
                {
                    var response = item.GetOrAddAsOverride(state.PatchMod);
                    string? responsetext = "";
                    if (response.Responses.TryGet(0, out var output)) responsetext = output.Text;
                    Console.WriteLine(response.FormKey+ " " + responsetext);

                    if (response.Flags == null) response.Flags = new();
                    response.Flags.Flags |= DialogResponses.Flag.Random;

                    var index = 0;
                    foreach (var condition in response.Conditions)
                    {
                        if (condition.Data != null)
                        {
                            var conditionFunction = (FunctionConditionData)condition.Data;
                            if (conditionFunction.Function == Condition.Function.GetPCIsSex)
                            {
                                if ((conditionFunction.ParameterOneNumber == 0 && condition.CompareOperator == CompareOperator.EqualTo) || (conditionFunction.ParameterOneNumber == 1 && condition.CompareOperator == CompareOperator.NotEqualTo))
                                {
                                    conditionFunction.Function = Condition.Function.GetGlobalValue;
                                    conditionFunction.ParameterOneRecord = He;
                                    conditionFunction.ParameterOneNumber = 1;
                                }
                                else if ((conditionFunction.ParameterOneNumber == 1 && condition.CompareOperator == CompareOperator.EqualTo) || (conditionFunction.ParameterOneNumber == 0 && condition.CompareOperator == CompareOperator.NotEqualTo))
                                {
                                    conditionFunction.Function = Condition.Function.GetGlobalValue;
                                    conditionFunction.ParameterOneRecord = She;
                                    conditionFunction.ParameterOneNumber = 1;
                                }
                                break;
                            }
                            else if (conditionFunction.Function == Condition.Function.GetIsSex && conditionFunction.RunOnType != Condition.RunOnType.Subject)
                            {
                                if (conditionFunction.Reference.FormKey == Constants.Player.FormKey)
                                {
                                    if ((conditionFunction.ParameterOneNumber == 0 && condition.CompareOperator == CompareOperator.EqualTo) || (conditionFunction.ParameterOneNumber == 1 && condition.CompareOperator == CompareOperator.NotEqualTo))
                                    {
                                        conditionFunction.Function = Condition.Function.GetGlobalValue;
                                        conditionFunction.ParameterOneRecord = He;
                                        conditionFunction.ParameterOneNumber = 1;
                                    }
                                    else if ((conditionFunction.ParameterOneNumber == 1 && condition.CompareOperator == CompareOperator.EqualTo) || (conditionFunction.ParameterOneNumber == 0 && condition.CompareOperator == CompareOperator.NotEqualTo))
                                    {
                                        conditionFunction.Function = Condition.Function.GetGlobalValue;
                                        conditionFunction.ParameterOneRecord = She;
                                        conditionFunction.ParameterOneNumber = 1;
                                    }
                                } else
                                {
                                    response.Conditions.Insert(index + 1, new ConditionFloat()
                                    {
                                        CompareOperator = CompareOperator.EqualTo,
                                        ComparisonValue = 0,
                                        Data = new FunctionConditionData()
                                        {
                                            Function = Condition.Function.HasKeyword,
                                            RunOnType = conditionFunction.RunOnType,
                                            ParameterOneRecord = ActorNonBinary,
                                        }
                                    });
                                    response.Conditions.Insert(index, new ConditionFloat() // is player
                                    {
                                        Flags = Condition.Flag.OR,
                                        CompareOperator = CompareOperator.EqualTo,
                                        ComparisonValue = 1,
                                        Data = new FunctionConditionData()
                                        {
                                            Function = Condition.Function.GetIsID,
                                            RunOnType = conditionFunction.RunOnType,
                                            ParameterOneRecord = Skyrim.Npc.Player,
                                        }
                                    });
                                    response.Conditions.Insert(index, new ConditionFloat() // not player
                                    {
                                        CompareOperator = CompareOperator.EqualTo,
                                        ComparisonValue = 0,
                                        Data = new FunctionConditionData()
                                        {
                                            Function = Condition.Function.GetIsID,
                                            RunOnType = conditionFunction.RunOnType,
                                            ParameterOneRecord = Skyrim.Npc.Player
                                        }
                                    });
                                    if ((conditionFunction.ParameterOneNumber == 0 && condition.CompareOperator == CompareOperator.EqualTo) || (conditionFunction.ParameterOneNumber == 1 && condition.CompareOperator == CompareOperator.NotEqualTo))
                                    {
                                        response.Conditions.Insert(index, new ConditionFloat()
                                        {
                                            Flags = Condition.Flag.OR,
                                            CompareOperator = CompareOperator.EqualTo,
                                            ComparisonValue = 1,
                                            Data = new FunctionConditionData()
                                            {
                                                Function = Condition.Function.GetGlobalValue,
                                                ParameterOneRecord = He
                                            }
                                        });
                                    }
                                    else
                                    {
                                        response.Conditions.Insert(index, new ConditionFloat()
                                        {
                                            Flags = Condition.Flag.OR,
                                            CompareOperator = CompareOperator.EqualTo,
                                            ComparisonValue = 1,
                                            Data = new FunctionConditionData()
                                            {
                                                Function = Condition.Function.GetGlobalValue,
                                                ParameterOneRecord = She
                                            }
                                        });
                                    }
                                }
                                break;
                            }
                        }
                        index++;
                    }
                }
            }
        }
    }
}

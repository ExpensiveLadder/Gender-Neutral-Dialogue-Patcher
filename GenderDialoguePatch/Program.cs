using System;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;
using Noggog;

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

        public static readonly FormLink<Global> He = FormKey.Factory("000CCC:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> She = FormKey.Factory("000CCD:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> They = FormKey.Factory("000CCE:Gender-Neutral Dialogue.esp").ToLink<Global>();

        public static bool IsThing(IDialogResponsesGetter response)
        {
            if (response.Conditions != null)
            {
                foreach (var condition in response.Conditions)
                {
                    if (condition.Data != null)
                    {
                        var conditionFunction = (FunctionConditionData)condition.Data.DeepCopy();
                        if (conditionFunction.Function == Condition.Function.GetPCIsSex)
                        {
                            return true;
                        }
                        else if (conditionFunction.Function == Condition.Function.GetIsSex && conditionFunction.RunOnType != Condition.RunOnType.Subject && conditionFunction.RunOnType != Condition.RunOnType.QuestAlias)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
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

                    //ExtendedList<Condition> conditionsToAdd = new();
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
                            else if (conditionFunction.Function == Condition.Function.GetIsSex && conditionFunction.RunOnType != Condition.RunOnType.Subject && conditionFunction.RunOnType != Condition.RunOnType.QuestAlias)
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
                                    condition.Flags |= Condition.Flag.OR;
                                    response.Conditions.Insert(index + 1, new ConditionFloat() // is player
                                    {
                                        Flags = Condition.Flag.OR,
                                        CompareOperator = CompareOperator.EqualTo,
                                        Data = new FunctionConditionData()
                                        {
                                            ParameterOneRecord = (IFormLink<ISkyrimMajorRecordGetter>)Constants.Player,
                                            Function = Condition.Function.GetIsID,
                                            RunOnType = conditionFunction.RunOnType
                                        }
                                    });
                                    response.Conditions.Insert(index + 2, new ConditionFloat() // not player
                                    {
                                        CompareOperator = CompareOperator.NotEqualTo,
                                        Data = new FunctionConditionData()
                                        {
                                            ParameterOneRecord = (IFormLink<ISkyrimMajorRecordGetter>)Constants.Player,
                                            Function = Condition.Function.GetIsID,
                                            RunOnType = conditionFunction.RunOnType
                                        }
                                    });
                                    if ((conditionFunction.ParameterOneNumber == 0 && condition.CompareOperator == CompareOperator.EqualTo) || (conditionFunction.ParameterOneNumber == 1 && condition.CompareOperator == CompareOperator.NotEqualTo))
                                    {
                                        response.Conditions.Insert(index + 3, new ConditionFloat()
                                        {
                                            Flags = Condition.Flag.OR,
                                            CompareOperator = CompareOperator.EqualTo,
                                            Data = new FunctionConditionData()
                                            {
                                                ParameterOneRecord = He,
                                                Function = Condition.Function.GetGlobalValue,
                                                RunOnType = conditionFunction.RunOnType,
                                                ParameterOneNumber = 1
                                            }
                                        });

                                    } else
                                    {
                                        response.Conditions.Insert(index + 3, new ConditionFloat()
                                        {
                                            Flags = Condition.Flag.OR,
                                            CompareOperator = CompareOperator.EqualTo,
                                            Data = new FunctionConditionData()
                                            {
                                                ParameterOneRecord = She,
                                                Function = Condition.Function.GetGlobalValue,
                                                RunOnType = conditionFunction.RunOnType,
                                                ParameterOneNumber = 1
                                            }
                                        });
                                    }
                                }
                                break;
                            }
                        }
                        index++;
                    }
                    /*
                    foreach (var condition in conditionsToAdd)
                    {
                        response.Conditions.Add(condition);
                    }
                    */
                }
                
            }
        }
    }
}

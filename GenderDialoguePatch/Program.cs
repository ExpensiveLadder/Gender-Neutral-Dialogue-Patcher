using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;

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

        public static readonly FormLink<Global> He = FormKey.Factory("000CCC:Gender-Neutral Dialogue.esp").AsLink<Global>();
        public static readonly FormLink<Global> She = FormKey.Factory("000CCD:Gender-Neutral Dialogue.esp").AsLink<Global>();
        public static readonly FormLink<Global> They = FormKey.Factory("000CCE:Gender-Neutral Dialogue.esp").AsLink<Global>();

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            //Your code here!
            foreach (var dialogueTopicGetter in state.LoadOrder.PriorityOrder.DialogTopic().WinningContextOverrides()) 
            {
                var dialogueTopic = dialogueTopicGetter.Record.DeepCopy();
                dialogueTopic.Responses.Clear();

                foreach (var dialogueResponseGetter in dialogueTopicGetter.Record.Responses)
                {
                    if (dialogueResponseGetter.Conditions != null)
                    {
                        bool copy = false;
                        var dialogueResponse = dialogueResponseGetter.DeepCopy();
                        foreach (var condition in dialogueResponse.Conditions)
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
                                        copy = true;
                                    } else if ((conditionFunction.ParameterOneNumber == 1 && condition.CompareOperator == CompareOperator.EqualTo) || (conditionFunction.ParameterOneNumber == 0 && condition.CompareOperator == CompareOperator.NotEqualTo)) {
                                        conditionFunction.Function = Condition.Function.GetGlobalValue;
                                        conditionFunction.ParameterOneRecord = She;
                                        conditionFunction.ParameterOneNumber = 1;
                                        copy = true;
                                    }
                                } /*else if (conditionFunction.Function == Condition.Function.GetIsSex && conditionFunction.RunOnType != Condition.RunOnType.Subject)
                                {
                                    if ((conditionFunction.ParameterOneNumber == 0 && condition.CompareOperator == CompareOperator.EqualTo) || (conditionFunction.ParameterOneNumber == 1 && condition.CompareOperator == CompareOperator.NotEqualTo))
                                    {
                                        conditionFunction.Function = Condition.Function.GetGlobalValue;
                                        conditionFunction.ParameterOneRecord = He;
                                        conditionFunction.ParameterOneNumber = 1;
                                        copy = true;
                                    }
                                    else if ((conditionFunction.ParameterOneNumber == 1 && condition.CompareOperator == CompareOperator.EqualTo) || (conditionFunction.ParameterOneNumber == 0 && condition.CompareOperator == CompareOperator.NotEqualTo))
                                    {
                                        conditionFunction.Function = Condition.Function.GetGlobalValue;
                                        conditionFunction.ParameterOneRecord = She;
                                        conditionFunction.ParameterOneNumber = 1;
                                        copy = true;
                                    }
                                }
                                */
                            }
                        }

                        if (copy == true)
                        {
                            Console.WriteLine(dialogueTopic.EditorID);
                            Console.WriteLine(dialogueResponse.EditorID);

                            if (dialogueResponse.Flags == null) dialogueResponse.Flags = new();
                            dialogueResponse.Flags.Flags |= DialogResponses.Flag.Random;

                            dialogueTopic.Responses.Add(dialogueResponse);
                            state.PatchMod.DialogTopics.Set(dialogueTopic);
                        }
                    }
                }
            }
        }
    }
}

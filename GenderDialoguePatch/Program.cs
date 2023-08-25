using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.WPF.Reflection.Attributes;
using Noggog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenderDialoguePatch
{
    public class TestSettings
    {
        [SettingName("Custom Pronouns")]
        public bool PatchCustomPronouns = false;

        [SettingName("Nominative")]
        public string CustomPronoun_Nominative = "they";

        [SettingName("Accusative")]
        public string CustomPronoun_Accusative = "them";

        [SettingName("Pronominal Possessive")]
        public string CustomPronoun_PronominalPossessive = "their";

        [SettingName("Predicative Possessive")]
        public string CustomPronoun_PredicativePossessive = "theirs";

        [SettingName("Reflexive")]
        public string CustomPronoun_Reflexive = "themself";
    }

    public class AliasInfos
    {
        public uint maxAliasID = 0;
        public List<QuestAlias> aliases = new();
        public bool overriden = false;
    }

    public class Program
    {
        static Lazy<TestSettings> Settings = null!;

        public static string CapatalizeFirst(string text)
        {
            return string.Concat(text[0].ToString().ToUpper(), text.AsSpan(1));
        }

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetAutogeneratedSettings(
                    nickname: "Settings",
                    path: "settings.json",
                    out Settings)
                .SetTypicalOpen(GameRelease.SkyrimSE, "YourPatcher.esp")
                .Run(args);
        }

        public static readonly FormLink<Global> Female = FormKey.Factory("000F48:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Global> Male = FormKey.Factory("000F49:Gender-Neutral Dialogue.esp").ToLink<Global>();
        public static readonly FormLink<Keyword> NpcNonBinary = FormKey.Factory("EBDA00:Update.esm").ToLink<Keyword>();
        public static readonly FormLink<GlobalShort> CustomPronouns = FormKey.Factory("000F4A:Gender-Neutral Dialogue.esp").ToLink<GlobalShort>();

        public static bool IsValidDialogue(IDialogResponsesGetter response)
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
                            if (conditionFunction.ParameterOneRecord.FormKey.GetHashCode() == NpcNonBinary.FormKey.GetHashCode()) return false;
                        }
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

        public static bool IsCustomDialogue(IDialogResponsesGetter response)
        {
            if (response.Conditions != null)
            {
                foreach (var condition in response.Conditions)
                {
                    if (condition.Data != null)
                    {
                        var conditionFunction = (FunctionConditionData)condition.Data.DeepCopy();
                        if (conditionFunction.Function == Condition.Function.GetGlobalValue)
                        {
                            if (conditionFunction.ParameterOneRecord.FormKey == CustomPronouns.FormKey)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static string TryCreateAlias(string textToReplace, string aliasName, FormKey reference, AliasInfos aliasInfos, string text)
        {
            if (text.Contains(textToReplace)) {
                text = text.Replace(textToReplace, "<Alias=" + aliasName + ">");
                if (!aliasInfos.aliases.Any(alias => alias.Name == "theirs"))
                {
                    aliasInfos.maxAliasID += 1;
                    aliasInfos.aliases.Add(new QuestAlias()
                    {
                        Name = aliasName,
                        ForcedReference = reference.ToNullableLink<IPlacedGetter>(),
                        Flags = QuestAlias.Flag.StoresText,
                        ID = aliasInfos.maxAliasID
                    });
                }
            }
            return text;
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            //Your code here!

            foreach (var item in state.LoadOrder.PriorityOrder.DialogResponses().WinningContextOverrides(state.LinkCache))
            {
                if (IsValidDialogue(item.Record))
                {
                    var responses = item.GetOrAddAsOverride(state.PatchMod);
                    string? responsetext = "";
                    if (responses.Responses.TryGet(0, out var output))
                    {
                        responsetext = output.Text;
                    }
                    Console.WriteLine(item.ModKey.FileName + " " + responses.FormKey + " " + responsetext);

                    responses.Flags ??= new();
                    responses.Flags.Flags |= DialogResponses.Flag.Random;

                    var index = 0;
                    foreach (var condition in responses.Conditions)
                    {
                        if (condition.Data == null) continue;
                        var conditionFunction = (FunctionConditionData)condition.Data;
                        if (conditionFunction.Function == Condition.Function.GetPCIsSex)
                        {
                            if ((conditionFunction.ParameterOneNumber == 0 && condition.CompareOperator == CompareOperator.EqualTo) || (conditionFunction.ParameterOneNumber == 1 && condition.CompareOperator == CompareOperator.NotEqualTo))
                            {
                                responses.Conditions.Remove(condition);
                                responses.Conditions.Insert(index, new ConditionFloat()
                                {
                                    CompareOperator = CompareOperator.EqualTo,
                                    ComparisonValue = 1,
                                    Data = new FunctionConditionData()
                                    {
                                        Function = Condition.Function.GetGlobalValue,
                                        ParameterOneRecord = Female
                                    }
                                });
                            }
                            else if ((conditionFunction.ParameterOneNumber == 1 && condition.CompareOperator == CompareOperator.EqualTo) || (conditionFunction.ParameterOneNumber == 0 && condition.CompareOperator == CompareOperator.NotEqualTo))
                            {
                                responses.Conditions.Remove(condition);
                                responses.Conditions.Insert(index, new ConditionFloat()
                                {
                                    CompareOperator = CompareOperator.EqualTo,
                                    ComparisonValue = 1,
                                    Data = new FunctionConditionData()
                                    {
                                        Function = Condition.Function.GetGlobalValue,
                                        ParameterOneRecord = Male
                                    }
                                });
                            }
                            break;
                        }
                        else if (conditionFunction.Function == Condition.Function.GetIsSex && conditionFunction.RunOnType != Condition.RunOnType.Subject)
                        {
                            if (conditionFunction.Reference.FormKey == Constants.Player.FormKey)
                            {
                                if ((conditionFunction.ParameterOneNumber == 0 && condition.CompareOperator == CompareOperator.EqualTo) || (conditionFunction.ParameterOneNumber == 1 && condition.CompareOperator == CompareOperator.NotEqualTo))
                                {
                                    responses.Conditions.Remove(condition);
                                    responses.Conditions.Insert(index, new ConditionFloat()
                                    {
                                        CompareOperator = CompareOperator.EqualTo,
                                        ComparisonValue = 1,
                                        Data = new FunctionConditionData()
                                        {
                                            Function = Condition.Function.GetGlobalValue,
                                            ParameterOneRecord = Female
                                        }
                                    });
                                }
                                else if ((conditionFunction.ParameterOneNumber == 1 && condition.CompareOperator == CompareOperator.EqualTo) || (conditionFunction.ParameterOneNumber == 0 && condition.CompareOperator == CompareOperator.NotEqualTo))
                                {
                                    responses.Conditions.Remove(condition);
                                    responses.Conditions.Insert(index, new ConditionFloat()
                                    {
                                        CompareOperator = CompareOperator.EqualTo,
                                        ComparisonValue = 1,
                                        Data = new FunctionConditionData()
                                        {
                                            Function = Condition.Function.GetGlobalValue,
                                            ParameterOneRecord = Male
                                        }
                                    });
                                }
                            }
                            else
                            {
                                responses.Conditions.Insert(index + 1, new ConditionFloat()
                                {
                                    CompareOperator = CompareOperator.EqualTo,
                                    ComparisonValue = 0,
                                    Data = new FunctionConditionData()
                                    {
                                        Function = Condition.Function.HasKeyword,
                                        RunOnType = conditionFunction.RunOnType,
                                        ParameterOneRecord = NpcNonBinary,
                                    }
                                });
                                responses.Conditions.Insert(index, new ConditionFloat() // is player
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
                                responses.Conditions.Insert(index, new ConditionFloat() // not player
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
                                    responses.Conditions.Insert(index, new ConditionFloat()
                                    {
                                        Flags = Condition.Flag.OR,
                                        CompareOperator = CompareOperator.EqualTo,
                                        ComparisonValue = 1,
                                        Data = new FunctionConditionData()
                                        {
                                            Function = Condition.Function.GetGlobalValue,
                                            ParameterOneRecord = Female
                                        }
                                    });
                                }
                                else
                                {
                                    responses.Conditions.Insert(index, new ConditionFloat()
                                    {
                                        Flags = Condition.Flag.OR,
                                        CompareOperator = CompareOperator.EqualTo,
                                        ComparisonValue = 1,
                                        Data = new FunctionConditionData()
                                        {
                                            Function = Condition.Function.GetGlobalValue,
                                            ParameterOneRecord = Male
                                        }
                                    });
                                }
                            }
                            break;
                        }
                        index++;
                    }
                }
                else if (Settings.Value.PatchCustomPronouns && IsCustomDialogue(item.Record))
                {
                    var responses = item.GetOrAddAsOverride(state.PatchMod);

                    foreach (var response in responses.Responses)
                    {
                        string? text = response.Text.String ?? throw new Exception("Null text for dialogue response");
                        text = text.Replace("&&p_Nominative&&", Settings.Value.CustomPronoun_Nominative);
                        text = text.Replace("&&P_Nominative&&", CapatalizeFirst(Settings.Value.CustomPronoun_Nominative));
                        text = text.Replace("&&p_Accusative&&", Settings.Value.CustomPronoun_Accusative);
                        text = text.Replace("&&P_Accusative&&", CapatalizeFirst(Settings.Value.CustomPronoun_Accusative));
                        text = text.Replace("&&p_Reflexive&&", Settings.Value.CustomPronoun_Reflexive);
                        text = text.Replace("&&P_Reflexive&&", CapatalizeFirst(Settings.Value.CustomPronoun_Reflexive));
                        text = text.Replace("&&p_Possessive&&", Settings.Value.CustomPronoun_PronominalPossessive);
                        text = text.Replace("&&P_Possessive&&", CapatalizeFirst(Settings.Value.CustomPronoun_PronominalPossessive));

                        response.Text = text;
                        Console.WriteLine(text);
                    }
                }
            }

            foreach (var questGetter in state.LoadOrder.PriorityOrder.Quest().WinningOverrides())
            {
                AliasInfos aliases = new();
                foreach (var aliasGetter in questGetter.Aliases)
                {
                    if (aliasGetter.ID > aliases.maxAliasID) aliases.maxAliasID = aliasGetter.ID;
                }
                foreach (var aliasGetter in questGetter.Aliases)
                {
                    Book? book = null;
                    if (aliasGetter.CreateReferenceToObject == null) continue;
                    if (aliasGetter.CreateReferenceToObject.Object.TryResolve<IBookGetter>(state.LinkCache, out var bookGetter))
                    {
                        if (bookGetter.Teaches is BookSpell) continue;
                        book = bookGetter.DeepCopy();
                    }
                    if (book == null) continue;
                    string text = book.BookText.ToString();
                    if (text == null) continue;

                    text = TryCreateAlias("<Alias.PronounPosObj=Player> does", "they do", FormKey.Factory("0008C6:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.PronounPosObjCap=Player> does", "They do", FormKey.Factory("0008D1:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.Pronoun=Player>'s", "they've", FormKey.Factory("0008C9:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.PronounCap=Player>'s", "They've", FormKey.Factory("000B53:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.Pronoun=Player> is", "they are", FormKey.Factory("000FD6:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.PronounCap=Player> is", "They are", FormKey.Factory("0008CB:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.Pronoun=Player> reaches", "they reach", FormKey.Factory("0008D2:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.PronounCap=Player> reaches", "They reach", FormKey.Factory("0008D3:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.Pronoun=Player> does", "they do", FormKey.Factory("0008CF:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.PronounCap=Player> does", "They do", FormKey.Factory("0008D0:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.Pronoun=Player> calls", "they call", FormKey.Factory("0008CD:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.PronounCap=Player> calls", "They call", FormKey.Factory("0008CE:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.Pronoun=Player> was", "they were", FormKey.Factory("000FD7:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.PronounCap=Player> was", "They were", FormKey.Factory("0008CA:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.Pronoun=Player>", "they", FormKey.Factory("000FD5:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.PronounCap=Player>", "They", FormKey.Factory("0008CC:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.PronounPos=Player>", "theirs", FormKey.Factory("0008C7:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.PronounPosCap=Player>", "Theirs", FormKey.Factory("000B52:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.PronounObj=Player>", "them", FormKey.Factory("0008C5:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.PronounObjCap=Player>", "Them", FormKey.Factory("000B51:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.PronounInt=Player>", "themself", FormKey.Factory("0008C8:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.PronounIntCap=Player>", "Themself", FormKey.Factory("000B32:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.PronounRef=Player>", "themself", FormKey.Factory("0008C8:Gender-Neutral Dialogue.esp"), aliases, text);
                    text = TryCreateAlias("<Alias.PronounRefCap=Player>", "Themself", FormKey.Factory("000B32:Gender-Neutral Dialogue.esp"), aliases, text);

                    if (aliases.overriden)
                    {
                        book.BookText = text;
                        state.PatchMod.Books.Set(book);
                        Console.WriteLine(/*book.ModKey.FileName + " " + */book.FormKey + " " + book.EditorID + Environment.NewLine + text);
                    }
                }
                if (aliases.overriden)
                {
                    var quest = questGetter.DeepCopy();
                    quest.Aliases.Add(aliases.aliases);
                    state.PatchMod.Quests.Set(quest);
                }
            }
        }
    }
}

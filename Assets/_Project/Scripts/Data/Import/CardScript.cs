using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Conditions;
using MultiverseCardBattle.Effects;
using MultiverseCardBattle.Targeting;

namespace MultiverseCardBattle.Data.Import
{
    /// <summary>
    /// ภาษาสั้นๆ สำหรับเขียนความสามารถของการ์ดในช่องเดียวของ CSV
    ///
    ///   action  := Trigger [เงื่อนไข,...] @Target[:Selection] -> effect; effect; ...
    ///   หลาย action คั่นด้วย |
    ///
    /// ตัวอย่าง
    ///   Play @EnemyUnit:Chosen -> DealDamage(2)
    ///   Play [Combo] @EnemyUnit:Chosen -> DealDamage(3) | Death @EnemyLeader -> DealDamage(2)
    ///   Play @AllEnemies -> ApplyStatus(Burn,1,3); DealDamage(1)
    ///
    /// ชื่อ Trigger / Target / Effect / Condition / Status / Keyword ใช้ชื่อ Enum
    /// ตัวพิมพ์เล็กใหญ่ ช่องว่าง _ - ไม่สำคัญ (Quick Attack = QuickAttack)
    /// </summary>
    public static class CardScript
    {
        // ---------- กลุ่ม Effect ตามรูปแบบอาร์กิวเมนต์ ----------

        // (value, duration)
        private static readonly HashSet<EffectType> ValueDuration =
            new HashSet<EffectType>
            {
                EffectType.Burn, EffectType.Regen,
                EffectType.Stun, EffectType.Silence
            };

        // (keyword+keyword)
        private static readonly HashSet<EffectType> KeywordArg =
            new HashSet<EffectType>
            {
                EffectType.AddKeyword, EffectType.RemoveKeyword
            };

        // (cardId)
        private static readonly HashSet<EffectType> CardArg =
            new HashSet<EffectType>
            {
                EffectType.Summon, EffectType.CreateCard
            };

        // ---------- แปลงข้อความเป็น Action ----------

        public static List<CardActionData> ParseActions(
            string text,
            Func<string, CardData> lookup,
            List<string> errors)
        {
            List<CardActionData> actions = new List<CardActionData>();

            if (string.IsNullOrWhiteSpace(text))
                return actions;

            foreach (string part in text.Split('|'))
            {
                string source = part.Trim();

                if (source.Length == 0)
                    continue;

                CardActionData action = ParseAction(source, lookup, errors);

                if (action != null)
                    actions.Add(action);
            }

            return actions;
        }

        private static CardActionData ParseAction(
            string source,
            Func<string, CardData> lookup,
            List<string> errors)
        {
            int arrow = source.IndexOf("->", StringComparison.Ordinal);

            if (arrow < 0)
            {
                errors.Add("action \"" + source + "\" has no '->'");
                return null;
            }

            string head = source.Substring(0, arrow).Trim();
            string effectsText = source.Substring(arrow + 2).Trim();

            CardActionData action = new CardActionData();

            int at = head.IndexOf('@');

            string left = at >= 0 ? head.Substring(0, at).Trim() : head;
            string right = at >= 0 ? head.Substring(at + 1).Trim() : "";

            // Trigger [conditions]
            int open = left.IndexOf('[');
            string triggerText = open >= 0 ? left.Substring(0, open).Trim() : left;

            if (!TryEnum(triggerText, out action.Trigger))
            {
                errors.Add("unknown trigger \"" + triggerText + "\" in \"" + source + "\"");
                return null;
            }

            bool ok = true;

            if (open >= 0)
            {
                int close = left.IndexOf(']', open);

                if (close < 0)
                {
                    errors.Add("missing ']' in \"" + source + "\"");
                    return null;
                }

                string conds = left.Substring(open + 1, close - open - 1);

                foreach (string cond in conds.Split(','))
                {
                    if (cond.Trim().Length == 0)
                        continue;

                    ConditionData data = ParseCondition(cond.Trim(), errors);

                    if (data == null)
                        ok = false;
                    else
                        action.Conditions.Add(data);
                }
            }

            // @Target[:Selection]
            if (right.Length > 0)
            {
                string[] targetParts = right.Split(':');

                if (!TryEnum(targetParts[0], out action.Target))
                {
                    errors.Add("unknown target \"" + targetParts[0] + "\" in \"" + source + "\"");
                    ok = false;
                }

                if (targetParts.Length > 1 &&
                    !TryEnum(targetParts[1], out action.Selection))
                {
                    errors.Add("unknown selection \"" + targetParts[1] + "\" in \"" + source + "\"");
                    ok = false;
                }
            }

            foreach (string effectText in effectsText.Split(';'))
            {
                string trimmed = effectText.Trim();

                if (trimmed.Length == 0)
                    continue;

                EffectData effect = ParseEffect(trimmed, lookup, errors);

                if (effect == null)
                    ok = false;
                else
                    action.Effects.Add(effect);
            }

            return ok ? action : null;
        }

        private static ConditionData ParseCondition(string text, List<string> errors)
        {
            string[] parts = text.Split(':');

            ConditionData data = new ConditionData();

            if (!TryEnum(parts[0], out data.Condition))
            {
                errors.Add("unknown condition \"" + parts[0] + "\"");
                return null;
            }

            if (parts.Length > 1 && !TryInt(parts[1], out data.Value))
            {
                errors.Add("condition \"" + text + "\" needs a whole number");
                return null;
            }

            return data;
        }

        private static EffectData ParseEffect(
            string text,
            Func<string, CardData> lookup,
            List<string> errors)
        {
            string name = text;
            string[] args = new string[0];

            int open = text.IndexOf('(');

            if (open >= 0)
            {
                int close = text.LastIndexOf(')');

                if (close < open)
                {
                    errors.Add("missing ')' in effect \"" + text + "\"");
                    return null;
                }

                name = text.Substring(0, open).Trim();

                string inner = text.Substring(open + 1, close - open - 1);

                args = inner.Trim().Length == 0
                    ? new string[0]
                    : inner.Split(',');

                for (int i = 0; i < args.Length; i++)
                    args[i] = args[i].Trim();
            }

            EffectData effect = new EffectData();

            if (!TryEnum(name, out effect.Type) || effect.Type == EffectType.None)
            {
                errors.Add("unknown effect \"" + name + "\"");
                return null;
            }

            if (effect.Type == EffectType.ApplyStatus)
                return ParseApplyStatus(effect, args, text, errors);

            if (effect.Type == EffectType.RemoveStatus)
            {
                if (args.Length < 1 || !TryEnum(args[0], out effect.Status))
                {
                    errors.Add("effect \"" + text + "\" needs a status name");
                    return null;
                }

                return effect;
            }

            if (effect.Type == EffectType.Transform)
            {
                if (args.Length < 1 || !ResolveCard(args[0], lookup, out effect.Card, errors))
                    return null;

                if (args.Length > 1 && !TryInt(args[1], out effect.Duration))
                {
                    errors.Add("effect \"" + text + "\" has a bad duration");
                    return null;
                }

                return effect;
            }

            if (CardArg.Contains(effect.Type))
            {
                if (args.Length > 0 &&
                    !ResolveCard(args[0], lookup, out effect.Card, errors))
                    return null;

                return effect;
            }

            if (KeywordArg.Contains(effect.Type))
            {
                if (args.Length < 1 || !TryKeywords(args[0], out effect.Keyword))
                {
                    errors.Add("effect \"" + text + "\" needs keyword names (join with +)");
                    return null;
                }

                return effect;
            }

            // (value) หรือ (value, duration)
            if (args.Length > 0 && !TryInt(args[0], out effect.Value))
            {
                errors.Add("effect \"" + text + "\" needs a whole number");
                return null;
            }

            if (args.Length > 1)
            {
                if (!ValueDuration.Contains(effect.Type))
                {
                    errors.Add("effect \"" + text + "\" takes only one number");
                    return null;
                }

                if (!TryInt(args[1], out effect.Duration))
                {
                    errors.Add("effect \"" + text + "\" has a bad duration");
                    return null;
                }
            }

            return effect;
        }

        private static EffectData ParseApplyStatus(
            EffectData effect,
            string[] args,
            string text,
            List<string> errors)
        {
            if (args.Length < 1 || !TryEnum(args[0], out effect.Status) ||
                effect.Status == StatusType.None)
            {
                errors.Add("effect \"" + text + "\" needs a status name");
                return null;
            }

            if (args.Length > 1 && !TryInt(args[1], out effect.Value))
            {
                errors.Add("effect \"" + text + "\" has a bad value");
                return null;
            }

            if (args.Length > 2 && !TryInt(args[2], out effect.Duration))
            {
                errors.Add("effect \"" + text + "\" has a bad duration");
                return null;
            }

            return effect;
        }

        private static bool ResolveCard(
            string id,
            Func<string, CardData> lookup,
            out CardData card,
            List<string> errors)
        {
            card = lookup != null ? lookup(id) : null;

            if (card == null)
            {
                errors.Add("unknown card \"" + id + "\"");
                return false;
            }

            return true;
        }

        // ---------- แปลง Action เป็นข้อความ ----------

        public static string Format(IList<CardActionData> actions)
        {
            if (actions == null || actions.Count == 0)
                return "";

            List<string> parts = new List<string>();

            foreach (CardActionData action in actions)
            {
                if (action != null)
                    parts.Add(FormatAction(action));
            }

            return string.Join(" | ", parts.ToArray());
        }

        private static string FormatAction(CardActionData action)
        {
            StringBuilder text = new StringBuilder();

            text.Append(action.Trigger);

            if (action.Conditions != null && action.Conditions.Count > 0)
            {
                List<string> conds = new List<string>();

                foreach (ConditionData condition in action.Conditions)
                {
                    if (condition == null)
                        continue;

                    conds.Add(condition.Value != 0
                        ? condition.Condition + ":" +
                          condition.Value.ToString(CultureInfo.InvariantCulture)
                        : condition.Condition.ToString());
                }

                text.Append(" [").Append(string.Join(", ", conds.ToArray())).Append(']');
            }

            text.Append(" @").Append(action.Target);

            if (action.Selection != TargetSelection.All)
                text.Append(':').Append(action.Selection);

            text.Append(" -> ");

            List<string> effects = new List<string>();

            if (action.Effects != null)
            {
                foreach (EffectData effect in action.Effects)
                {
                    if (effect != null)
                        effects.Add(FormatEffect(effect));
                }
            }

            text.Append(string.Join("; ", effects.ToArray()));

            return text.ToString();
        }

        private static string FormatEffect(EffectData effect)
        {
            List<string> args = new List<string>();

            if (effect.Type == EffectType.ApplyStatus)
            {
                args.Add(effect.Status.ToString());
                args.Add(Number(effect.Value));
                args.Add(Number(effect.Duration));
            }
            else if (effect.Type == EffectType.RemoveStatus)
            {
                args.Add(effect.Status.ToString());
            }
            else if (effect.Type == EffectType.Transform)
            {
                args.Add(effect.Card != null ? effect.Card.CardID : "");
                args.Add(Number(effect.Duration));
            }
            else if (CardArg.Contains(effect.Type))
            {
                if (effect.Card != null)
                    args.Add(effect.Card.CardID);
            }
            else if (KeywordArg.Contains(effect.Type))
            {
                args.Add(KeywordList(effect.Keyword, "+"));
            }
            else
            {
                args.Add(Number(effect.Value));

                if (ValueDuration.Contains(effect.Type))
                    args.Add(Number(effect.Duration));
            }

            // ตัดเลข 0 ท้ายสุดทิ้ง (ยกเว้นอาร์กิวเมนต์แรกของสถานะ/การ์ด/Keyword)
            int keep = args.Count;

            while (keep > 0 && args[keep - 1] == "0" &&
                   (effect.Type != EffectType.ApplyStatus || keep > 1))
            {
                keep--;
            }

            // Transform ต้องมีการ์ดเสมอ แม้ไม่มี Duration
            if (effect.Type == EffectType.Transform && keep < 1)
                keep = 1;

            if (keep == 0)
                return effect.Type.ToString();

            args.RemoveRange(keep, args.Count - keep);

            return effect.Type + "(" + string.Join(",", args.ToArray()) + ")";
        }

        private static string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        // ---------- ตัวช่วยแปลงค่า (ใช้ร่วมกับ CardImporter) ----------

        public static string Normalize(string text)
        {
            StringBuilder name = new StringBuilder();

            foreach (char c in text)
            {
                if (c == ' ' || c == '_' || c == '-')
                    continue;

                name.Append(char.ToLowerInvariant(c));
            }

            return name.ToString();
        }

        /// <summary>
        /// แปลงชื่อเป็น Enum (ไม่สนตัวพิมพ์/ช่องว่าง) และไม่รับตัวเลขล้วน
        /// </summary>
        public static bool TryEnum<T>(string text, out T value) where T : struct
        {
            value = default(T);

            if (string.IsNullOrWhiteSpace(text))
                return false;

            string wanted = Normalize(text);

            foreach (string name in Enum.GetNames(typeof(T)))
            {
                if (Normalize(name) == wanted)
                {
                    value = (T)Enum.Parse(typeof(T), name);
                    return true;
                }
            }

            return false;
        }

        public static bool TryInt(string text, out int value)
        {
            return int.TryParse(
                text.Trim(), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out value);
        }

        /// <summary>แปลงรายการ Keyword คั่นด้วย + | หรือ , เป็น Flags</summary>
        public static bool TryKeywords(string text, out Keyword keywords)
        {
            keywords = Keyword.None;

            if (string.IsNullOrWhiteSpace(text))
                return true;

            foreach (string part in text.Split('+', '|', ','))
            {
                if (part.Trim().Length == 0)
                    continue;

                Keyword keyword;

                if (!TryEnum(part, out keyword) || keyword == Keyword.None)
                    return false;

                keywords |= keyword;
            }

            return true;
        }

        public static bool TryTags(string text, out CardTag tags)
        {
            tags = CardTag.None;

            if (string.IsNullOrWhiteSpace(text))
                return true;

            foreach (string part in text.Split('+', '|', ','))
            {
                if (part.Trim().Length == 0)
                    continue;

                CardTag tag;

                if (!TryEnum(part, out tag) || tag == CardTag.None)
                    return false;

                tags |= tag;
            }

            return true;
        }

        public static string KeywordList(Keyword keywords, string separator)
        {
            List<string> names = new List<string>();

            foreach (Keyword keyword in Enum.GetValues(typeof(Keyword)))
            {
                if (keyword != Keyword.None && (keywords & keyword) == keyword)
                    names.Add(keyword.ToString());
            }

            return string.Join(separator, names.ToArray());
        }

        public static string TagList(CardTag tags, string separator)
        {
            List<string> names = new List<string>();

            foreach (CardTag tag in Enum.GetValues(typeof(CardTag)))
            {
                if (tag != CardTag.None && (tags & tag) == tag)
                    names.Add(tag.ToString());
            }

            return string.Join(separator, names.ToArray());
        }
    }
}

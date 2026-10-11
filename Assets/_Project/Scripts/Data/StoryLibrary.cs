using System.Collections.Generic;

namespace MultiverseCardBattle.Data
{
    /// <summary>บทสนทนา 1 บรรทัด (Speaker ว่าง = ผู้บรรยาย)</summary>
    public class DialogueLine
    {
        public string Speaker;
        public string Text;

        public DialogueLine(string speaker, string text)
        {
            Speaker = speaker;
            Text = text;
        }
    }

    /// <summary>เนื้อเรื่องของด่านหนึ่ง: ก่อนสู้ (Intro) และหลังชนะครั้งแรก (Outro)</summary>
    public class StoryBeat
    {
        public string StageId;
        public List<DialogueLine> Intro = new List<DialogueLine>();
        public List<DialogueLine> Outro = new List<DialogueLine>();
    }

    /// <summary>1 บทของ Story Mode ครอบคลุม 2 ด่าน และให้รางวัลเมื่อจบบท</summary>
    public class StoryChapter
    {
        public int Number;
        public string Title;
        public string Synopsis;

        public List<string> StageIds = new List<string>();

        public int RewardGold;
        public int RewardDust;

        // เล่นหลังชนะด่านสุดท้ายของบท
        public List<DialogueLine> Epilogue = new List<DialogueLine>();
    }

    /// <summary>
    /// เนื้อเรื่อง Story Mode 5 บท (ใช้ด่านเดิมของ Campaign 10 ด่าน บทละ 2 ด่าน)
    /// </summary>
    public static class StoryLibrary
    {
        public const string Guide = "Lumen";
        public const string Hero = "You";

        private static List<StoryChapter> chapters;
        private static List<StoryBeat> beats;

        public static IReadOnlyList<StoryChapter> Chapters
        {
            get
            {
                Build();
                return chapters;
            }
        }

        public static StoryBeat GetBeat(string stageId)
        {
            Build();

            foreach (StoryBeat beat in beats)
            {
                if (beat.StageId == stageId)
                    return beat;
            }

            return null;
        }

        public static StoryChapter ChapterOf(string stageId)
        {
            Build();

            foreach (StoryChapter chapter in chapters)
            {
                if (chapter.StageIds.Contains(stageId))
                    return chapter;
            }

            return null;
        }

        public static StoryChapter GetChapter(int number)
        {
            Build();

            foreach (StoryChapter chapter in chapters)
            {
                if (chapter.Number == number)
                    return chapter;
            }

            return null;
        }

        private static DialogueLine L(string speaker, string text)
        {
            return new DialogueLine(speaker, text);
        }

        private static StoryBeat Beat(
            string stageId, DialogueLine[] intro, DialogueLine[] outro)
        {
            StoryBeat beat = new StoryBeat();

            beat.StageId = stageId;
            beat.Intro.AddRange(intro);
            beat.Outro.AddRange(outro);

            beats.Add(beat);

            return beat;
        }

        private static StoryChapter Chapter(
            int number, string title, string synopsis,
            string stageA, string stageB,
            int gold, int dust, params DialogueLine[] epilogue)
        {
            StoryChapter chapter = new StoryChapter();

            chapter.Number = number;
            chapter.Title = title;
            chapter.Synopsis = synopsis;
            chapter.StageIds.Add(stageA);
            chapter.StageIds.Add(stageB);
            chapter.RewardGold = gold;
            chapter.RewardDust = dust;
            chapter.Epilogue.AddRange(epilogue);

            chapters.Add(chapter);

            return chapter;
        }

        private static void Build()
        {
            if (chapters != null)
                return;

            chapters = new List<StoryChapter>();
            beats = new List<StoryBeat>();

            // ---------- บท 1: The Rift Opens ----------

            Chapter(1, "The Rift Opens",
                "You wake in a place between worlds with nothing but a deck of cards.",
                "stage_01", "stage_02", 150, 60,
                L(Guide, "Two fights, and you already move like a Summoner."),
                L(Guide, "But the Rift is still growing. Worlds are leaking into each other."),
                L(Hero, "Then we keep going. Where does the road lead?"),
                L(Guide, "Into the dark, I'm afraid. Follow the whispers."));

            Beat("stage_01",
                new[]
                {
                    L("", "Cold light. A cracked arena floating in a sea of stars."),
                    L(Hero, "Where... am I?"),
                    L(Guide, "Awake at last! I'm Lumen, spirit of the Card Archive. Welcome to the Rift."),
                    L(Guide, "Worlds that were never meant to meet are colliding. Heroes and monsters fall through the cracks, and they all fight with cards."),
                    L(Guide, "You hold a Summoner's deck. Before anything else, you must learn to use it."),
                    L("Training Dummy Captain", "Fresh arrivals line up here! Show me what you can do, rookie!")
                },
                new[]
                {
                    L("Training Dummy Captain", "Ha! Not bad at all. Hardly a scratch on you."),
                    L(Guide, "Draw, play, attack. That's the heart of it. Everything else is clever timing."),
                    L(Guide, "Take these cards. The Rift always rewards those who stand back up.")
                });

            Beat("stage_02",
                new[]
                {
                    L("", "A dusty road cuts through the ruins of a world half-swallowed by the Rift."),
                    L(Guide, "Bandits. The Rift scrambles the minds of ordinary people, and they take what they can."),
                    L("Bandit Chief", "A traveler with a shiny deck! Hand it over and we might let you walk."),
                    L(Hero, "I've had a long day. Try me.")
                },
                new[]
                {
                    L("Bandit Chief", "Okay, okay! I yield! Take your reward and leave!"),
                    L(Guide, "Fast, aggressive decks fall apart when you hold your ground. Remember that."),
                    L(Guide, "Now come. Something is moving in the alleys ahead, and it knows we're here.")
                });

            // ---------- บท 2: Whispers in the Shadows ----------

            Chapter(2, "Whispers in the Shadows",
                "A city of stealth and faith hides a secret about the Sovereign.",
                "stage_03", "stage_04", 180, 80,
                L(Guide, "Every shadow in this city whispered one name: the Multiverse Sovereign."),
                L(Hero, "Who is he?"),
                L(Guide, "A mage who learned the Rift can be ruled instead of feared. He collects heroes' echoes to rebuild the worlds in his image."),
                L(Guide, "The mages' tower is next. They guard his research.") );

            Beat("stage_03",
                new[]
                {
                    L("", "Neon-less streets, wet stone, and the soft click of boots that stop when you stop."),
                    L(Guide, "Stealth and poison. Never reach into a dark corner without checking it first."),
                    L("Night Broker", "You carry the Archive's mark. The Sovereign pays well for people like you."),
                    L(Hero, "Pays for what?"),
                    L("Night Broker", "For delivery. Alive, preferably. Let's see how alive you are.")
                },
                new[]
                {
                    L("Night Broker", "Tch... Fine. Take the cards. And a free warning: the temple already knows your name."),
                    L(Hero, "Everyone seems to know my name except me."),
                    L(Guide, "Hm. Let's find out why.")
                });

            Beat("stage_04",
                new[]
                {
                    L("", "A white temple rises over the rooftops. Its gates are sealed by walls of shields and prayer."),
                    L("High Priestess", "Summoner. The Sovereign promised an end to endless war. You would drag the worlds back into it."),
                    L(Hero, "I'd rather the worlds choose for themselves."),
                    L("High Priestess", "Then break my wall, if you can. Heal, endure, and believe.")
                },
                new[]
                {
                    L("High Priestess", "A wall can be broken. A promise like his cannot be... not so easily."),
                    L("High Priestess", "Go to the Mage Tower. Learn what the Sovereign truly builds there."),
                    L(Guide, "She may be an enemy, but she isn't lying. Let's move.")
                });

            // ---------- บท 3: Arcane and Iron ----------

            Chapter(3, "Arcane and Iron",
                "Spellcasters and titans guard the road to the Sovereign.",
                "stage_05", "stage_06", 220, 100,
                L(Guide, "Magic and iron both broke against your deck."),
                L(Hero, "The Archmage called it the Gate Engine. What was he building?"),
                L(Guide, "A way to pull entire worlds through the Rift at once. If the Sovereign finishes it, nothing stays in its own universe."),
                L(Guide, "We have to reach the peak before that happens.") );

            Beat("stage_05",
                new[]
                {
                    L("", "Chains of lightning crawl up the black spire. Every window glows a different colour."),
                    L("Archmage", "Intruder. Do you know how long this Engine took to build? Years of study, centuries of stolen magic."),
                    L(Hero, "I know it makes the Rift worse."),
                    L("Archmage", "Worse for you, perhaps. Burn, stun, silence. Let's see how you cast under pressure.")
                },
                new[]
                {
                    L("Archmage", "Impossible... you read my spells before I cast them."),
                    L(Guide, "The Engine's core is beneath his study. Take his notes, they might be useful."),
                    L(Guide, "Ahead is the Titan Gate. Whatever's beyond it, it's loud.")
                });

            Beat("stage_06",
                new[]
                {
                    L("", "A gate taller than mountains. Beyond it, the ground shakes with each distant step."),
                    L("Gatekeeper", "No passage! The Sovereign's orders. Nobody gets by the Titans."),
                    L(Guide, "Huge bodies, sweeping damage. Don't overextend your units."),
                    L(Hero, "Then I'll just have to be smarter than a wall.")
                },
                new[]
                {
                    L("Gatekeeper", "The gate... holds no more. Go, then. Maybe the Sovereign was wrong about you."),
                    L(Hero, "What's he afraid of?"),
                    L(Guide, "A Summoner who can't be controlled. Let's keep it that way.")
                });

            // ---------- บท 4: Across the Horizon ----------

            Chapter(4, "Across the Horizon",
                "Pirate fleets and ancient dragons stand between you and the peak.",
                "stage_07", "stage_08", 260, 120,
                L(Guide, "Dragons, pirates... the Rift keeps handing you strange company."),
                L(Hero, "Strange company who respect a good fight."),
                L(Guide, "Every defeated champion adds a card to your deck, and a piece to the Sovereign's fear."),
                L(Guide, "Only two more trials. Then the Sovereign himself.") );

            Beat("stage_07",
                new[]
                {
                    L("", "Snow, wind and thunder. At the top of the world, something enormous opens one golden eye."),
                    L("Ancient Dragon Lord", "A Summoner climbs my mountain. Few reach the peak. Fewer leave."),
                    L(Guide, "Heavy hitters and big bodies. Control the board and don't give it free attacks."),
                    L(Hero, "It's just a really big dragon. I've dealt with worse today.")
                },
                new[]
                {
                    L("Ancient Dragon Lord", "Mm. You fought with patience, not fury. That is rare."),
                    L("Ancient Dragon Lord", "The sea beyond my mountain hides a captain who cannot be tamed. Be careful."),
                    L(Guide, "Pirates next, then. Keep your hand close.")
                });

            Beat("stage_08",
                new[]
                {
                    L("", "A storm-lashed sea. A ship with a skull-grinning sail cuts across the horizon."),
                    L("Pirate Emperor", "Gahaha! A Summoner on my sea! Fight me, and the treasure is yours!"),
                    L(Guide, "Rubber limbs, swordsmen, and a lot of noise. They hit hard and fast."),
                    L(Hero, "Then we hit first.")
                },
                new[]
                {
                    L("Pirate Emperor", "Gahaha! Fantastic! That's the best fight I've had in ages!"),
                    L("Pirate Emperor", "A word, Summoner. The Sovereign's trial is waiting in the world of the Soul Reapers. Don't blink."),
                    L(Guide, "Almost there. One last trial before the final door.")
                });

            // ---------- บท 5: The Multiverse Sovereign ----------

            Chapter(5, "The Multiverse Sovereign",
                "Heroes and gods from every universe gather for the final duel.",
                "stage_09", "stage_10", 400, 200,
                L(Guide, "It's over. The Rift is quiet."),
                L(Hero, "Is it really over?"),
                L(Guide, "The Rift will never close completely. But worlds can choose to meet instead of collide."),
                L(Guide, "Thank you, Summoner. The Archive is yours, and every card in it is waiting for the next duel.") );

            Beat("stage_09",
                new[]
                {
                    L("", "A pale city of spirit blades. A single captain waits on a bridge of white stone."),
                    L("Captain of the Gotei 13", "You've come far, Summoner. Show me whether you deserve to face the Sovereign."),
                    L(Guide, "Spirit blades, stand power and a very angry substitute reaper. Stay sharp."),
                    L(Hero, "I'm ready. Let's duel.")
                },
                new[]
                {
                    L("Captain of the Gotei 13", "You've earned it. The final gate stands open."),
                    L("Captain of the Gotei 13", "He does not rule, only fears. Remember that when you face him."),
                    L(Guide, "This is it. Whatever comes next, I'm with you.")
                });

            Beat("stage_10",
                new[]
                {
                    L("", "The Rift opens into a hall made of every sky at once. On a throne of fractured worlds sits one figure."),
                    L("The Multiverse Sovereign", "You came all this way to stop me. How sweet."),
                    L(Hero, "I came to ask a question. Why?"),
                    L("The Multiverse Sovereign", "Because worlds die. Heroes die. I would make something that never ends: one universe, every hero, one will."),
                    L(Hero, "That's not a universe. That's a cage."),
                    L("The Multiverse Sovereign", "Then prove it. Gods and champions of every world, answer my call!")
                },
                new[]
                {
                    L("The Multiverse Sovereign", "A cage... You saw it clearly. I only wanted nothing to be lost."),
                    L(Hero, "Things get lost. That's why we cherish them."),
                    L("The Multiverse Sovereign", "Perhaps. Go, Summoner. Build something better than my fear.")
                });
        }
    }
}

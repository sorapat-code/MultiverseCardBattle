# ฐานข้อมูลการ์ดแบบ CSV (Card Database)

ระบบนี้ให้คุณเพิ่ม/แก้การ์ดด้วย Excel หรือ Google Sheets แทนการเขียนโค้ด แล้วให้เกมอ่านตอนเริ่ม

## วิธีใช้งาน

1. ใน Unity เลือกเมนู **Multiverse Card Battle > Card Database > Export Code Cards To CSV**
   จะได้ `Assets/_Project/Resources/CardDatabase/starter_cards.csv` (การ์ดเดิมทั้งหมด)
2. เปิดไฟล์ด้วย Excel / Google Sheets แล้วแก้หรือเพิ่มแถว (ดูตัวอย่างที่ `docs/cards_template.csv`)
3. บันทึกเป็น **CSV UTF-8** วางไว้ในโฟลเดอร์ `Assets/_Project/Resources/CardDatabase/` (ไฟล์ `.csv` กี่ไฟล์ก็ได้)
4. เลือกเมนู **Card Database > Validate And Reload** ดูผลใน Console (Error / Warning / สรุป)
5. กด Play ได้เลย

ลำดับการโหลด: การ์ด asset ใน `Resources/Cards` → การ์ดจาก CSV → การ์ดในโค้ด (`StarterCardLibrary`)
ถ้า ID ซ้ำ **ตัวที่โหลดก่อนชนะ** ดังนั้นแถว CSV จะทับการ์ดในโค้ดที่ ID เดียวกัน

แถวที่ผิดจะถูกข้ามและรายงานเป็น Error (แถวอื่นยังโหลดตามปกติ) ส่วน Warning คือเตือนเฉย ๆ

## คอลัมน์

| คอลัมน์ | ความหมาย |
|---|---|
| ID | รหัสการ์ด ห้ามซ้ำ (alias: CardID) |
| Name | ชื่อการ์ด |
| Type | `Unit`, `Spell`, `Equipment`, `Leader` |
| Rarity | `Common`, `Rare`, `Epic`, `Legendary`, `Secret` |
| Universe | `Original`, `Naruto`, `OnePiece`, `Bleach`, `DragonBall`, `JoJo`, `WutheringWaves`, `HonkaiStarRail`, `Fate`, `JujutsuKaisen` |
| Faction | `Neutral`, `LeafVillage`, `SandVillage`, `MistVillage`, `CloudVillage`, `StoneVillage`, `Akatsuki`, `Otsutsuki`, `StrawHatPirates`, `RedHairPirates`, `BlackbeardPirates`, `Marines`, `WorldGovernment`, `Revolutionaries`, `SoulSociety`, `Gotei13`, `Arrancar`, `Wandenreich`, `ZFighters`, `FriezaForce`, `RedRibbonArmy`, `StardustCrusaders`, `Passione`, `Morioh`, `DioFollowers`, `Chaldea`, `AstralExpress`, `StellaronHunters`, `IPC`, `XianzhouAlliance`, `Jinzhou`, `BlackShores`, `Fractsidus`, `JujutsuHigh`, `CursedSpirits` |
| Tags | แยกด้วย `|` `,` หรือ `+` |
| Cost / Attack / Health | ตัวเลข (alias: Atk, HP) |
| Keywords | แยกด้วย `|` `,` หรือ `+` |
| Abilities | สคริปต์ความสามารถ (ดูด้านล่าง) |
| ImmuneAmount | ลดความเสียหายทุกครั้งที่โดน เมื่อการ์ดมี Keyword Immune (ว่าง = 3) เช่น Immune 3 โดนตี 5 เหลือ 2 |
| AuraAttack / AuraHealth | ออร่าบัฟพวกเดียวกัน (ไม่รวมตัวเอง) |
| EvolvesInto / EvolveAfterTurns | ID การ์ดที่จะวิวัฒน์เป็น และจำนวนเทิร์นของเจ้าของที่อยู่บนสนาม |
| Source | `Pack`, `Campaign`, `Event`, `Craft`, `Shop`, `Reward`, `Starter`, `Token`, `Debug` (ว่าง = Token ถ้ามี Tag Token ไม่งั้น Pack) |
| Description | ข้อความอธิบาย (ว่างได้ ระบบสร้างให้จากความสามารถ) |

ชื่อ enum ไม่สนตัวพิมพ์เล็กใหญ่ ช่องว่าง `_` `-`

## ภาษาความสามารถ (Abilities)

```
Trigger [เงื่อนไข, เงื่อนไข:ค่า] @Target[:Selection] -> Effect(args); Effect(args)
```
หลายความสามารถคั่นด้วย `|` เช่น
`Play @EnemyUnit:Chosen -> DealDamage(2) | Death @Self -> DrawCard(1)`

ถ้าไม่ใส่ `@Target` จะเป็น Self

### รูปแบบ args ของ Effect

| รูปแบบ | ใช้กับ |
|---|---|
| `(ค่า)` | DealDamage, Heal, GainAttack, DrawCard ฯลฯ |
| `(ค่า,รอบ)` | Burn, Regen, Stun, Silence |
| `(Keyword+Keyword)` | AddKeyword, RemoveKeyword |
| `(CardID)` | Summon, CreateCard |
| `(CardID,รอบ)` | Transform (รอบ > 0 = แปลงชั่วคราว) |
| `(Status,ค่า,รอบ)` | ApplyStatus |
| `(Status)` | RemoveStatus |

### ชื่อที่ใช้ได้

- **Trigger**: `BattleStart`, `BattleEnd`, `TurnStart`, `TurnEnd`, `Draw`, `Play`, `Summon`, `Discard`, `Death`, `Attack`, `Hit`, `DamageTaken`, `Heal`, `LeaderAttack`, `OnTarget`, `Spellburst`
- **Target**: `Self`, `AllyUnit`, `AllyLeader`, `EnemyUnit`, `EnemyLeader`, `AnyUnit`, `AnyLeader`, `AllAllies`, `AllEnemies`, `AllUnits`
- **Selection**: `All`, `Chosen`, `Random`
- **Effect**: `DealDamage`, `DealTrueDamage`, `Heal`, `GainAttack`, `GainHealth`, `SetAttack`, `SetHealth`, `ReduceCost`, `IncreaseCost`, `SetCost`, `DrawCard`, `DiscardCard`, `CreateCard`, `DestroyCard`, `Summon`, `ReturnToHand`, `MoveToDeck`, `Banish`, `AddKeyword`, `RemoveKeyword`, `Silence`, `Stun`, `GainEnergy`, `SpendEnergy`, `Discover`, `Transform`, `Burn`, `Regen`, `ApplyStatus`, `RemoveStatus`, `Adapt`
- **Condition**: `TurnNumber`, `IsFirstTurn`, `SelfHealthLessThan`, `SelfHealthGreaterThan`, `EnergyGreaterThan`, `EnergyLessThan`, `HandCount`, `DeckCount`, `GraveyardCount`, `AllyUnitCount`, `EnemyUnitCount`, `HasTag`, `HasFaction`, `HasUniverse`, `Chance`, `Combo`, `TargetMarked`
- **Status**: `Stun`, `Silence`, `Burn`, `Regen`, `Freeze`, `Root`, `Disarm`, `Confusion`, `Poison`, `Bleed`, `Curse`, `Marked`, `AttackUp`, `AttackDown`, `DefenseUp`, `DefenseDown`, `Vulnerable`, `Weakness`, `Barrier`, `CostUp`, `CostDown`, `Exposed`, `Protected`, `Blessed`, `Transformed`, `TemporaryTransformation`, `TransformationLock`, `TransformationFatigue`
- **Keyword**: `Taunt`, `Charge`, `Rush`, `Shield`, `Lifesteal`, `Backstab`, `Stealth`, `DoubleStrike`, `Pierce`, `QuickAttack`, `Overwhelm`, `Unstoppable`, `Guard`, `Ward`, `Immune`, `Reborn`, `Spellburst`, `Shadowbound`, `Fast`, `Enrage`, `Retaliate`, `Frenzy`, `Elusive`, `Regenerate`, `Thorns`, `LethalStrike`, `Overload`, `Echo`
- **Tag**: `Human`, `Beast`, `Machine`, `Spirit`, `Undead`, `God`, `Demon`, `Dragon`, `Melee`, `Ranged`, `Magic`, `Support`, `Tank`, `Assassin`, `Leader`, `Elite`, `Boss`, `Summon`, `Token`, `Fire`, `Water`, `Wind`, `Earth`, `Lightning`, `Ice`, `Dark`, `Light`

## ตัวอย่าง

```
Taunt:        Keywords = Taunt
Battlecry:    Play @EnemyUnit:Chosen -> DealDamage(2)
Deathrattle:  Death -> Summon(EX_T01)
Combo:        Play [Combo] @EnemyLeader -> DealDamage(3)
Poison:       Play @EnemyUnit:Chosen -> ApplyStatus(Poison,2,3)
Transform:    TurnStart [SelfHealthLessThan:15] -> Transform(TKN_003)
Adapt:        Play -> Adapt(1)
```
ดูตัวอย่างครบทุกแบบใน `docs/cards_template.csv`

## เพิ่ม Universe / Faction ใหม่

Universe และ Faction เป็น enum ในโค้ด (`Scripts/Cards/Universe.cs` และ `Faction.cs`) ต้อง **เพิ่มชื่อใหม่ต่อท้ายสุด** ของ enum
(ห้ามแทรกกลาง เพราะ asset เดิมเก็บเป็นตัวเลข) แล้วค่อยใช้ชื่อนั้นใน CSV ได้

## Error ที่เจอบ่อย

| ข้อความ | สาเหตุ |
|---|---|
| unknown Rarity/Universe/... | สะกดชื่อ enum ผิด |
| duplicate ID | ID ซ้ำกับแถวอื่นใน CSV |
| unknown card reference | CardID ใน Summon/Transform/EvolvesInto ไม่มี |
| bad ability script | ลืม `->` หรือวงเล็บไม่ครบ |
| missing required column | ไม่มีหัวคอลัมน์ ID/Name |

## Secret card, Dust and achievements

- `Sung Jin-Woo, Shadow Monarch` (`EVT_001`, rarity `Secret`, source `Event`) is not collectible:
  it is hidden from the Collection, packs, Discover and the deck builder. It can only appear when a
  **Summon** effect that creates a new card is used by the human player (5% chance per summoned card,
  `EffectExecutor.SecretCardChance`).
- `Artorias the Abysswalker` (`EVT_002`) is a Secret/Unique card unlocked by the "A Knight's Duty"
  condition (`Data/SecretCardService.cs`): win a match in which you win 4+ unit combats as the attacker,
  lose 3+ units, never drop below 10 Leader HP and never use an instant-destroy effect. Match stats are
  tracked by `Battle/BattleStats.cs`. Unlocking shows a banner before the result summary, adds 1 copy to the
  Collection and grants the hidden achievement (500 Gold + 300 Dust). Card-specific passives use `CardPassive`.
- `Ainz Ooal Gown, The Supreme Overlord` (`EVT_003`) is unlocked by "The Throne of Nazarick": in one match play 5+ Spells,
  kill 4+ enemy units with Spells/card abilities (not combat) and win with Leader HP >= 15. Mechanics: the `Doom` status
  (destroys at the start of the caster's next turn), `CardPassive` flags (Grasp Heart / Dark Wisdom / Supreme Magic Caster /
  Undead Overlord) and `BattleManager.PendingTarget` (the player clicks the Grasp Heart target at turn start). Kill causes are
  tracked via `BattleManager.ActiveEffectSource` and `CardInstance.LastEffectSource`.
- `Vergil, The Alpha and the Omega` (`EVT_004`) is unlocked by "The Power of the Yamato": win a match without playing any
  Spell in which one unit attacks 3 times in total without dying. Judgment Cut uses the `GrantExtraAttack` effect; Concentration,
  Devil Trigger, Sin Devil's Regeneration and Overwhelming Power are `CardPassive` flags. The **DEVIL TRIGGER** button
  (blue flames, right column of the battle screen) sends `DevilTriggerCommand`: once per match, during your turn but not the
  turn Vergil is summoned. It turns Vergil into `TKN_006` (Awakened Dark Slayer) for 2 of your turns, keeping position,
  damage and attacks used; on revert HP is clamped to Vergil's normal max.
- Disenchant gives Dust (Common 5, Rare 20, Epic 100, Legendary 400). Crafting costs Dust
  (Common 40, Rare 100, Epic 400, Legendary 1600). Copies used by any deck are protected.
- Achievements live in `Data/AchievementService.cs`; stats are stored in the save file (version 3).
- A player whose deck reaches 0 cards loses the battle immediately.

## Card art

Put card images in `Assets/_Project/Resources/CardArt/{CardID}.png` (loaded by `Data/CardArtLoader.cs`, no Sprite import
settings needed). Reserved secret-card art: `EVT_001` Sung Jin-Woo, `EVT_002` Artorias, `EVT_003` Ainz Ooal Gown,
`EVT_004` Vergil, `TKN_006` Vergil Devil Trigger form (Ainz/Vergil cards are not implemented yet).

## Equipment (อุปกรณ์)

- `Type = Equipment` ใช้ Effect `Equip` (สวมยูนิตพวกเราที่เลือก ได้โบนัสถาวร ATK/HP/Keyword จาก `GearAttack/GearHealth/GearKeywords`) หรือ `EquipLeader` (ช่องอุปกรณ์ Leader อยู่ได้ `GearDurability` เทิร์น ทำงานตอน TurnStart/TurnEnd ของเจ้าของ)
- ยูนิตที่ตาย → อุปกรณ์หายไปพร้อมกัน, Leader gear ที่ถูกแทนที่/หมดอายุลงสุสาน
- การ์ดอุปกรณ์ปัจจุบัน 8 ใบ (`EQP_001`–`EQP_008`, `BuildEquipment()` ใน `StarterCardLibrary.Roster.cs`, ไม่เกิน Epic): ยูนิต 5 ใบ = Iron Katana, Guardian Plate, Windrunner Boots, Crimson Fang, Moonlit Blade / Leader 3 ใบ = Healer's Charm, Scholar's Monocle, Arcane Brazier ได้จากรางวัลด่าน 1–7 และ 9 เพิ่มใบใหม่ได้ด้วย helper `UnitGear` / `LeaderGear` หรือแถว CSV ที่ `Type=Equipment` ตัวอย่างการทดสอบอยู่ที่ `Tests/PlayMode/EquipmentTests.cs`

## Keyword Backstab (เดิมชื่อ Poison)

ยูนิตที่โดนความเสียหายจากการ์ดที่มี `Backstab` จะถูกทำลาย (ใช้ใน CSV/ScriptableObject เป็น `Backstab` แทน `Poison`) ส่วนสถานะ `Poison` (ดาเมจต่อเนื่อง) ยังเป็นสถานะตามเดิม

## ชุดการ์ด (CardSet)

- `Core` (ซองละ 100 Gold) และ `Rift` (ซองละ 150 Gold) แยกกันที่ `CardData.Set`
- การ์ดชุดเก่า (STR/SPL/RFT/CHN/TGT/GRV/LKT ฯลฯ) ถูกลบทั้งหมดแล้ว เหลือเฉพาะ **การ์ดลับ 4 ใบ + ร่าง Devil Trigger** (ไม่แตะ) และ Token / ไม้ตาย Leader / การ์ดบอสที่ระบบอื่นต้องใช้
- ซอง `Rift` ยังไม่มีการ์ด จึงซ่อนจากร้านค้าอัตโนมัติ (ใส่การ์ดใหม่แล้วตั้ง `Set = CardSet.Rift` ซองจะกลับมาเอง)

## ชุดการ์ดปัจจุบัน (Roster)

เขียนอยู่ที่ `Data/StarterCardLibrary.Roster.cs` (ตัวประกอบ 26 + เวทมนตร์ 15 + อุปกรณ์ 8 = 49 ใบ) ตัวละครสำคัญระดับ Legendary/Secret เจ้าของโปรเจกต์เพิ่มเอง
ตัวประกอบทุกใบไม่เกิน Epic (มีเทสต์ตรวจ `RosterTests`)

- **ตัวประกอบ `SUP_001`–`SUP_026`** (ยูนิต 26 ใบ จาก 16 จักรวาล): Common 11 / Rare 10 / Epic 5 — เช่น Konohamaru, Rock Lee, Usopp, Chopper, Krillin, Connie, Sasha, March 7th, Chixia, Kobeni, Crestfallen Warrior (Common); Nami, Uryu, Nobara, Mash, Zenitsu, Power, Lady, Solaire, Cha Hae-In, Polnareff (Rare); Igris, Albedo, Shalltear, Rengoku, Gaara (Epic)
- **เวทมนตร์ `MAG_001`–`MAG_015`** (15 ใบ ใส่ Tag ธาตุเพื่อให้เสียง/เอฟเฟกต์เลือกตามธาตุ): Spark, Frost Shard, Healing Rain, Gale Slash, Stone Wall, Scholar's Notes, Chain Lightning, Shadow Bind, Blessing of Dawn, Spellbreaker (Fast), Emergency Ward (Fast), Inferno, Rewind Fate, World's End (Legendary), Flame Dart
- **เอฟเฟกต์เสริมใน `EffectData`** (ใช้ในตัวประกอบบางใบ): `BonusValue` + `BonusIfHealthAtMost` กับ `DealDamage` = ถ้าเป้าหมายยูนิตมี HP ไม่เกินค่านี้ (ตรวจก่อนโดนดาเมจ) ดาเมจเพิ่ม — ใช้กับ `SUP_003` Usopp (1 ดาเมจ, +1 ถ้า HP ≤ 2); `DrawnSpellDiscount` กับ `DrawCard` = ถ้าจั่วได้ Spell ลด Cost ลงจนจบเทิร์นของเจ้าของ (สถานะ `CostDown` อายุ 1) — ใช้กับ `SUP_012` Nami; เทสต์อยู่ที่ `Tests/PlayMode/RosterEffectTests.cs`
- เด็คตั้งต้น (30 ใบ), เด็คศัตรู/รางวัลของด่าน 1–10, และ Tutorial ใช้การ์ดชุดนี้ (`StarterDecks`, `CampaignLibrary`, `TutorialSetup`)
- การ์ดทุกใบต้องได้มาจากด่านหรือเด็คตั้งต้นเสมอ (มีเทสต์ `EveryCollectibleCard_CanBeObtained`) — เพิ่มการ์ดใหม่แล้วอย่าลืมใส่เป็นรางวัลด่านหรือในเด็คตั้งต้น

### รูปการ์ด

รูปของ SUP_/MAG_/EQP_ สร้างด้วยสคริปต์ `tools/generate_card_art.py` (Python + Pillow + numpy, 640x480) สไตล์อนิเมะ: ตัวละครเป็นภาพครึ่งตัวหน้าอนิเมะ (ตา ผม เงาเซล) ส่วนเวท/อุปกรณ์เป็นวงหมุนรอยปาดพลังเวท โค้ดวาดแยกอยู่ใน `tools/cardart/` (`fx` แสง/รอยปาด, `magic` เวท+อุปกรณ์, `portrait` ตัวละคร, `characters` ค่าตั้งของตัวละครแต่ละคน)
รันใหม่ได้: `python3 tools/generate_card_art.py` (ระบุ ID ท้ายคำสั่งเพื่อสร้างเฉพาะใบนั้น, `--keep` ข้ามใบที่มีไฟล์แล้ว)
อยากใช้รูปจริง: วางไฟล์ PNG ชื่อ `{CardID}.png` ใน `Resources/CardArt/` ทับได้เลย

## Story Mode

- แทนที่หน้า Campaign: 5 บท บทละ 2 ด่าน (ใช้ด่าน `stage_01`–`stage_10` และเซฟเดิม) เนื้อเรื่องอยู่ที่ `StoryLibrary` (Intro ก่อนสู้ครั้งแรก, Outro หลังชนะครั้งแรก, Epilogue ท้ายบท)
- รางวัลจบบทแจกครั้งเดียวต่อบท (`SaveData.StoryChaptersRewarded`) ด่านที่ผ่านแล้วกดปุ่ม "Story" เพื่ออ่านซ้ำได้

## Keyword ใหม่และระบบเชน (Chain)

| Keyword | ผล |
|---|---|
| `Fast` | เฉพาะ Spell: เล่นตอบโต้การกระทำของศัตรูได้แม้ในเทิร์นศัตรู ใช้ Energy ที่เหลือของตัวเอง |
| `Enrage` | HP ไม่เต็ม = +2 Attack |
| `Retaliate` | สวนกลับเสมอเมื่อโดนยูนิตโจมตี แม้ตายจากการโจมตีนั้น / ผู้โจมตีมี Quick Attack / ถูก Stun, Freeze, Disarm |
| `Frenzy` | รอดจากความเสียหายครั้งแรก = +2 Attack ถาวร (ครั้งเดียว) |
| `Elusive` | ศัตรูเลือกเป็นเป้าหมายไม่ได้ ขณะเจ้าของมียูนิตอื่นที่ไม่ใช่ Elusive |
| `Regenerate` | ฟื้น 2 HP ตอนเริ่มเทิร์นเจ้าของ |
| `Thorns` | ถูกยูนิตโจมตี = สะท้อน 2 ดาเมจกลับ แม้ตาย |
| `LethalStrike` | ทำดาเมจแล้วเป้าหมายเหลือ HP ไม่เกิน 3 = ตายทันที |
| `Overload` | ลงการ์ดแล้ว Energy เทิร์นหน้าถูกล็อก (`CardData.OverloadAmount`, 0 = 1) |
| `Echo` | ใช้แล้วได้สำเนา (ไม่มี Echo) กลับเข้ามือ 1 ใบ |

เอฟเฟกต์ใหม่: `Negate` (ใช้กับ Target `Self`) ยกเลิกการกระทำที่กำลังถูกตอบโต้ ใช้ได้เฉพาะตอนตอบโต้

### เชน (Chain)

1. ผู้เล่นลงการ์ดหรือประกาศโจมตี: จ่าย Energy แล้วการกระทำถูกวางลงเชน
2. คู่ต่อสู้ที่มี Fast spell ที่เล่นได้จะได้ตอบโต้ (มนุษย์เลือกเล่นหรือ PASS, AI ตัดสินเอง) ตอบโต้ซ้อนกันได้ไม่จำกัด (ขีดจำกัดความปลอดภัย `BattleManager.MaxChainDepth` = 12)
3. เมื่อไม่มีใครตอบโต้เพิ่ม ใบหลังสุดทำงานก่อน แล้วไล่ลงมาถึงการกระทำแรก (LIFO) ถ้าเป้าหมายหายไปแล้วการกระทำนั้นจะ fizzle
4. ถ้าคู่ต่อสู้ไม่มี Fast spell ที่เล่นได้ การกระทำทำงานทันทีเหมือนเดิม

การ์ดตัวอย่าง: `CHN_001`-`CHN_027` (ใน Core Pack)

## Trigger ใหม่: ถูกเล็งเป้า / ลงสุสาน และเอฟเฟกต์ดูหัวกอง

### ถูกเล็งเป้า (`OnTarget`)
ทำงานเมื่อการ์ดบนสนามถูกเลือกเป็นเป้าหมายของ **การโจมตี** หรือ **Spell / ความสามารถ** (Battlecry ที่เลือกเป้า, Deathrattle แบบ Random ฯลฯ) ตั้งตัวกรองได้ต่อ Action:

- `TargetedBy`: `Any` / `Attack` / `SpellOrAbility`
- `TargetedFrom`: `Any` / `Enemy` / `Ally` (ผู้เล็งเป้าเป็นฝ่ายไหนเมื่อเทียบกับเจ้าของการ์ด)
- Target rule ใหม่ `EventSource` = การ์ดต้นเหตุ (ผู้โจมตี / การ์ดที่เล็งเป้ามา) ใช้ได้เมื่อยังอยู่บนสนาม
- สคริปต์ CSV: `OnTarget(Attack,Enemy) @EventSource -> DealDamage(2)`
- ตัวอย่าง: `TGT_001`-`TGT_007`

### ลงสุสาน (`Graveyard`)
ทำงานเมื่อการ์ดถูกส่งลงสุสาน **จากทางใดก็ได้**: ตายบนสนาม, ถูกทิ้งจากมือ, มือเต็มแล้วถูกเผา, ถูกทำลาย และ Spell ที่ใช้เสร็จ (แยกจาก Deathrattle ที่ทำงานเฉพาะตอนตายบนสนาม)
- สคริปต์ CSV: `Graveyard @EnemyLeader -> DealDamage(2)`
- ตัวอย่าง: `GRV_001`-`GRV_007`

### ดูหัวกอง (`LookTop`)
`LookTop(จำนวน[, ประเภท[, ค่าใช้จ่ายสูงสุด]])` ดูหัวกอง X ใบ แล้วเลือกหยิบ 1 ใบเข้ามือ
- ประเภท: `Any` / `Unit` / `Spell` / `Equipment` (ค่าเริ่มต้น `Any`) ค่าใช้จ่ายสูงสุด 0 = ไม่จำกัด
- แสดงเฉพาะใบที่เข้าเงื่อนไข ถ้าไม่มีเลยจะไม่เกิดอะไร ใบที่เปิดดูแต่ไม่ได้เลือกจะไปอยู่ล่างกอง
- AI เลือกใบที่ค่าใช้จ่ายสูงสุดในตัวเลือก
- ตัวอย่าง: `LKT_001`-`LKT_007`, `GRV_006` (ลงสุสานแล้วดูหัวกอง 3 ใบ หยิบ Spell)

## เอฟเฟกต์การ์ดลับ (SecretFx)

โค้ดอยู่ที่ `Scripts/Screens/SecretFx.cs` ใช้กับการ์ดที่ `Rarity = Secret` และร่าง Devil Trigger (`TKN_006`) แต่ละใบมีสีประจำตัว (Jin-Woo ม่วง / Artorias ทอง / Ainz แดง / Vergil น้ำเงิน)

- **ปลดล็อก:** แสงวาบเต็มจอ + เวทวงกลม และตัวการ์ดพลิกหมุนเข้ามากระแทกกลางจอพร้อมเสาแสง คลื่น อนุภาค (`CardReveal`)
- **ลงสนาม:** แสงวาบ เสาแสง คลื่นกระแทก รอยฟัน จอสั่น และชื่อการ์ดเด้งขึ้นกลางจอ (`SecretFx.Arrive`)
- **ใช้สกิล:** โจมตี / ตาย ของการ์ดลับมีเอฟเฟกต์เฉพาะตัวละคร (`SecretFx.Skill`) และ Devil Trigger มีเอฟเฟกต์แปลงร่างเต็มรูปแบบ (`SecretFx.DevilTrigger`)
- **Collection / Deck Builder / ในมือ:** กรอบเรืองแสงกะพริบ แสงสะท้อนพาดผ่านรูป และประกายลอยจากขอบการ์ด (`SecretAura` ติดกับ `CardView` อัตโนมัติ)

### ฉากไซเนมาติก (`SecretCinematic`)
ฉากเต็มจอยาว ~5-8 วินาที กดที่ไหนก็ได้เพื่อข้าม: แถบจอกว้าง ไฟสาดส่อง รัศมีแสงหมุน การ์ดพลิกกระแทกกลางจอพร้อมจอสั่น ชื่อพิมพ์ทีละตัวอักษร ฝนประกาย และจังหวะกระแทกหลายช่วง
- **ปลดล็อกการ์ดลับ:** เล่นจนจบแล้วค้างที่การ์ด คลิกอีกครั้งเพื่อไปต่อ
- **การ์ดลับลงสนาม:** เล่นฉากก่อน แล้วการ์ดบินไปยังช่องของมันบนสนาม (ระหว่างนั้นล็อกการควบคุมและหยุด AI)
- **Devil Trigger:** ฉากแปลงร่างเต็มจอ แล้วต่อด้วยเอฟเฟกต์บนการ์ด
- สกิลโจมตี/ตายของการ์ดลับมี hit-stop (หยุดเวลาสั้นๆ) แสงวาบ และเสาแสงเพิ่ม

**ขอบเขตของเอฟเฟกต์การ์ดลับ:** ใช้กับการ์ดที่ `Rarity = Secret`, การ์ดที่มี Keyword `Unique` ทุกใบ และร่าง Devil Trigger ทั้งหมดนี้อัตโนมัติ รวมการ์ดที่เพิ่มในอนาคต (เช็คที่ `SecretFx.IsSecret`) การ์ดที่ไม่มีสีประจำตัวจะได้สีคงที่จากรหัสการ์ด

## Per-character cutscenes (CharacterScene)

`SecretCinematic` normally plays one shared timeline (only tint/epithet differ). Characters with their own
choreography derive from `CharacterScene` (black screen, letterbox, subtitle, timed `At(time, action)` steps;
`SecretCinematic` still owns the card flip, title typing and fly-to-slot). `CharacterScene.Create` picks the
scene by card ID and mode; returns null -> shared timeline is used.

| Card | Scene | Idea |
|---|---|---|
| Vergil `EVT_004` | `VergilScene` | Judgment Cut: moon, eye slit (card art, red/cyan split), sheathe click, accelerating slash cage, freeze, shatter + X |
| Sung Jin-Woo `EVT_001` | `JinWooScene` | Shadow army eyes open faster and faster, ARISE slam, eyes flare, ground rings + smoke, soldiers bow, violet pillar |
| Artorias `EVT_002` | `ArtoriasScene` | Fog + embers, greatsword drops and plants, golden shaft, single wide sweep, sword dissolves |
| Ainz `EVT_003` | `AinzScene` | Counter-rotating red magic circles with orbiting lights, red eyes, three domination pulses, collapse, explosion + red pillars |
| Devil Trigger `TKN_006` (Transform mode) | `DevilTriggerScene` | Accelerating heartbeat + lightning, freeze, radial speed-line explosion, title |

Shared parts: `RingAnim`, `SlamText`, `EyePair`, `VergilCut` (slash line, optional palette), `SwordDrop`.
Not yet compiled/run in Unity.

## Coin toss (CoinToss)

When `BattleSetup.UseCoinFlip` is on, `BattleScreen` plays `CoinToss` right after `StartBattle` (before mulligan).
The result is already decided by `BattleManager`; the overlay only shows it: the coin arcs up, flips (front = YOU in
gold, back = opponent in silver), lands with a bounce, then both name plates show 1ST / 2ND (second player: +1 card and
The Coin) with a slam banner. AI, input and mulligan wait until it ends; click skips to the result, click again (or
~8 s) continues. Not yet compiled/run in Unity.

## In-battle Settings (BattleSettingsPanel)

The battle screen's top-right button is now **Settings** (was Concede). It opens `BattleSettingsPanel`: Master/Music/Effects
sliders, Mute All, Animation Speed, Opponent Speed and **Surrender** (needs a second confirm, then counts as a defeat).
Esc also opens it when nothing is selected/dragged/open (Esc closes it too). The game (AI + input) is paused while it is
open. Values are the same `GameSettings` as the main-menu Options and are saved. Not yet compiled/run in Unity.

## Leaders (LeaderData / LeaderSystem)

A Leader is chosen before the match (main menu -> "Leader: ..." button; stored in `SaveData.LeaderId`, default Goku,
empty = no Leader). Each Leader has **one active skill** (button on the battle screen, once per turn) and **a finisher
card** that joins the hand when the Leader's condition is met. There is no passive skill by design.

- Data: `LeaderLibrary` / `LeaderData`. Battle side: `BattlePlayer.Leader / LeaderPower / LeaderSkillUsed /
  LeaderFinisherGiven`, `LeaderSystem` (Power from events, grants the finisher), `LeaderSkillCommand` (the skill).
- Power: each Leader has a `PowerGain` flag set (`LeaderDamaged`, `UnitDied`, `UnitPlayed`, `SpellPlayed`, `EnemyUnitDied`, `SkillUsed`); every matching event gives +1. The skill itself is picked by `LeaderSkillKind` in `LeaderSkillCommand`.
  At `PowerMax` the finisher is added to the hand once per match (waits if the hand is full).
- **Son Goku** (`LDR_001`, Dragon Ball): Kaioken - 1 Energy + 2 Leader HP: all your units get +2 Attack until end of
  turn. Ki 10 -> **Spirit Bomb** (`TKN_SPIRIT_BOMB`, cost 4 Spell): 10 damage to the enemy Leader, 5 to every enemy
  unit, heal your Leader 5.
- **Naruto Uzumaki** (`LDR_002`): Shadow Clone - 2 Energy: summon two 1/1 Shadow Clones. Chakra 8 (+1 per unit played from
  hand, +1 per skill) -> **Tailed Beast Bomb** (cost 5 Spell): 6 damage to the enemy Leader and every enemy unit.
- **Satoru Gojo** (`LDR_003`): Infinity - 2 Energy: all your units get a 3-point Barrier until the end of your next turn.
  Cursed Energy 8 (+1 per spell, +1 per skill) -> **Hollow Purple** (cost 5 Spell): 10 damage to a chosen enemy unit,
  6 to the enemy Leader.
- **Tony Stark** (`LDR_004`, `Universe.Original` because there is no Marvel universe): Repulsor - 2 Energy: 1 damage to
  every enemy unit and the enemy Leader. Arc Reactor 8 (+1 per spell, +1 per enemy unit death) -> **Proton Cannon**
  (cost 4 Spell): 9 damage to the enemy Leader, 2 to every enemy unit.
- Skills are not targeted (one button, no target picking). Only the player has a Leader for now (the enemy side has none). Known quirk: Kaioken's HP cost goes through
  `DamageLeader`, so it is counted in `BattleStats.LeaderDamage` of the opponent.
Not yet compiled/run in Unity.

## Font & Text Size

- All UI text uses **Inter** (SIL OFL 1.1), bundled in `Assets/_Project/Resources/Fonts/` (Regular / Bold / Italic + license). `UIFactory.ApplyFont` picks the real bold/italic files instead of faux styles. If the files are missing it falls back to Segoe UI / Arial.
- **Text Size** (Options → Normal 100% / Large 115% / Extra Large 130%) is stored in the save (`SaveData.TextScale`) and multiplies every font size in `UIFactory.CreateText`. Changing it reloads the main menu; other screens pick it up when they are built.
- Button labels use best-fit so larger text shrinks to fit instead of being cut off.

## Boss Leaders (Story mode)

The enemy has a Leader only on the **last stage of each chapter** (`CampaignStage.EnemyLeaderId` -> `BattleSetup.EnemyLeaderId`
-> `LeaderLibrary.Get`). Boss Leaders have `PlayerSelectable = false` so they never appear in the Leader menu. The AI uses the
skill through `AIPlanner.PlanLeaderSkill` (after playing cards, before attacking, only when it is useful) and plays its finisher
like any card in hand. Skills are generic (`LeaderSkillKind`: BuffAttack, Barrier, Heal, SummonTokens, DamageEnemies,
WeakenEnemies, with `SkillValue/SkillCount/SkillTokenId/SkillHitsLeader`).

| Stage | Boss | Skill | Power (max) | Finisher |
|---|---|---|---|---|
| stage_02 | Bandit Chief | Raid Orders (1 Energy): all units +1 Attack this turn | Loot 6: unit played, skill | Ambush! (3): 5 damage to your Leader |
| stage_04 | High Priestess | Sanctuary Prayer (2): heal her Leader and units 2 | Faith 6: her Leader damaged, skill | Divine Judgment (4): 4 to all your units, heals her 6 |
| stage_06 | Gatekeeper | Titan Stomp (3): 2 damage to all your units | Fortitude 6: his Leader damaged, his unit died | Gate Collapse (5): 6 to all your units |
| stage_08 | Pirate Emperor | Conqueror's Haki (2): your units deal half damage until the end of your next turn | Haki 7: Leader damaged, unit played, skill | Emperor's Wrath (4): 7 to your Leader, 3 to all your units |
| stage_10 | The Multiverse Sovereign | Echo of Heroes (3): summon two 2/2 Hero Echoes | Fracture 8: any unit died, skill | World Rewrite (6): 12 to your Leader, 4 to all your units |

Readability: both Leader panels have a Power bar and an **i** button that opens `LeaderInfoPanel` (skill, cost, Power
condition with the current value, finisher card text). The boss intro opens automatically at the start of a boss stage
(game paused until "Begin the battle"). When the boss uses its skill, or its finisher joins its hand, a banner announces it
(the player cannot see the enemy hand). Numbers are first-pass tuning.

## Battle screen layout (Legends-of-Runeterra style)

`BattleScreen.BuildUI` / `BuildBoard`: forest frame background, a green grass board (`BoardArt.Grass`) with glowing edges
(pink = enemy side, cyan = your side, brighter on that side's turn), a centre line, Energy gems on the left (red = enemy,
blue = you), a round End Turn dial on the right, the enemy Leader top-centre, your Leader bottom-left, the hand centred under
the board. Card preview + keyword hints float on the left only while hovering a card. The Battle Log is a drawer opened by the
**Log** button. A "YOUR TURN / OPPONENT'S TURN" banner (`TurnBanner`) plays on every turn change. All art is generated by code
in `BoardArt` (no image files).

`BoardDecor` adds the stone frame, gold gem ornaments, bushes/rocks around the board and an animated atmosphere clipped to the
board (light rays, a passing breeze, drifting fog, fireflies, falling leaves). All procedural, unscaled time.

## หน้าต่อสู้แบบโต๊ะ 3D (BattleStage3D)

- `Screens/BattleStage3D.cs` สร้างฉาก 3D: กล้อง (fov 36 เอียงมองโต๊ะ), โต๊ะ/กรอบหิน, พุ่มไม้, แสง+เงา+หมอก, หิ่งห้อย/ใบไม้ (ParticleSystem) และแสงเรืองฝั่งที่เป็นเทิร์น (`SetTurnGlow`)
- การ์ดทุกใบ = `CardHolder3D` (World Space Canvas ขนาดพิกเซล x 0.01 + กล่องบางเป็นตัวการ์ด) ข้างในคือ `CardView` เดิม จึงคลิก/ชี้/ลากผ่าน EventSystem เดิมได้ (ชี้แล้วการ์ดลอยเข้าหากล้อง)
- เมนู, HUD, Leader, ปุ่ม, พรีวิว, ghost ตอนลาก ยังเป็น UI 2D ซ้อนทับ; ตำแหน่ง FX แปลงด้วย `UiFx.ScreenOf`
- ปล่อยการ์ดลงสนามเช็คด้วย ray-plane (`IsOverBoard`); จอแคบกว่า 16:9 กล้องถอยออกอัตโนมัติ
- ใช้ Built-in pipeline (Standard shader ถูกใส่ใน Always Included Shaders)

### กราฟิกโต๊ะ 3D รอบตกแต่ง (BattleStage3D.Scenery / StagePostFx)

- เสาหินคริสตัลเรืองแสงสี่มุม (ชมพูศัตรู/ฟ้าเรา สว่างตามเทิร์น), วงรูนกลางสนามหมุนช้า, กรอบช่องวางยูนิต 7 ช่องต่อฝั่ง, ลำแสงลอดใบไม้, หมอกพื้น, ละอองแสง
- โพสต์เอฟเฟกต์เขียนเองด้วย `Resources/MCBPostFx.shader` + `StagePostFx` (Bloom สองระดับ, ปรับสี, วิกเน็ต) ไม่ต้องติดตั้ง package; เชดเดอร์เรืองแสง `Resources/MCBGlow.shader`
- กล้องขยับตามเมาส์เล็กน้อยและสั่นตอนโดนตี (`stage.Shake`); การ์ดมีขอบทอง ลอยเบาๆ และเอียงตามเมาส์ตอนชี้

### Asset ภายนอก (CC0)

- โมเดลต้นไม้/พุ่ม/หิน/ดอกไม้ใน `Assets/_Project/Resources/Nature` มาจาก **Quaternius – Ultimate Stylized Nature Pack** (CC0 1.0, สาธารณสมบัติ) ดึงจาก repo `tilakpatell/tilakverse-assets` (texture ย่อเหลือ 512px) ใช้เชดเดอร์ `MCB/FoliageCutout`; ไม่ต้องใส่เครดิตแต่ใส่ไว้เพื่อให้เกียรติ: https://quaternius.com
- `BattleStage3D.Nature.cs` วางต้นสน/ต้นไม้/พุ่ม/หินสองข้างและหลังโต๊ะ (ถ้าหาโมเดลไม่เจอจะถอยไปใช้ทรงกลมแบบเดิม)

### ธีมโต๊ะตามด่าน (StageTheme)

`Screens/StageTheme.cs` กำหนดแสง หมอก สีพื้นสนาม/โต๊ะ ละออง และโทนสีโพสต์เอฟเฟกต์ ตามด่านใน Campaign
(Quick Battle / ด่าน 1 = ป่ายามสาย): 2 Bandit Road ฝุ่นทองบ่ายแก่, 3 Shadow Alley กลางคืนนีออน, 4 Temple Siege หินทรายเรืองทอง,
5 Mage Tower ม่วงอาร์เคน, 6 Titan Gate หินเทาหมอกหนา, 7 Dragon Peak ภูเขาไฟประกายไฟ, 8 Grand Line ดาดฟ้าเรือทะเลสด,
9 Soul Reaper's Trial จันทร์ซีดกลีบซากุระ, 10 Multiverse จักรวาลม่วงทอง ธีมที่ไม่มีต้นไม้ (8, 10) จะไม่วางโมเดลธรรมชาติ

## ระดับความยาก AI (AiDifficulty)

- เลือกได้ที่ปุ่ม **Difficulty** ในเมนูหลัก (Easy / Normal / Hard) เก็บใน `SaveData.AiLevel` แล้วส่งผ่าน `BattleSetup.Difficulty` → `AIPlanner.Difficulty` ตอนเริ่มแมตช์
- **Normal** = พฤติกรรมเดิมทุกอย่าง (เทสต์เดิมไม่เปลี่ยน). **Easy**: ลงการ์ดสุ่มและข้ามบางใบ, บางครั้งไม่โจมตี/เลือกเป้าหมายมั่ว, ใช้สกิล Leader ไม่ค่อยเป็น, ตอบโต้ Fast spell น้อย, HP Leader ศัตรู x0.8, Gold ที่ชนะ x0.7. **Hard**: คำนวณ "ตีรวมจบเกม" (lethal) ก่อนทุกครั้ง, ให้ความสำคัญกับเป้าหมายอันตราย (Lifesteal/DoubleStrike/Pierce/Overwhelm), ตอบโต้เกือบทุกครั้ง, Mulligan ใบ Cost 4+, HP Leader ศัตรู x1.25, Gold ที่ชนะ x1.5
- ตัวคูณทั้งหมดอยู่ใน `Data/AiDifficulty.cs` (`AiDifficultyInfo`)

## สถิติและประวัติแมตช์ (StatsService)

- `Data/StatsService.cs` บันทึกทุกแมตช์ (ไม่รวม Tutorial) เป็น `MatchRecord` ใน `SaveData.History` (เก็บล่าสุด 50 รายการ) พร้อมสถิติรวม: ชนะ/แพ้/เสมอ, สตรีคปัจจุบัน/สูงสุด, สถิติแยกตามระดับความยาก, จำนวนการ์ดที่ลง, ยูนิตที่กำจัด, ดาเมจใส่ Leader, การ์ดที่ใช้บ่อยสุด (`CardUsage`)
- เรียกจาก `CampaignService.OnBattleFinished` หน้าสรุปผลแสดงรีแคปแมตช์ + สตรีค; ดูทั้งหมดได้ที่ปุ่ม **Stats & History** ในเมนูหลัก. `SaveData.CurrentVersion` = 5 (เซฟเก่าเปิดได้ ค่าใหม่เริ่มจากศูนย์)

## เพลงและเสียงบรรยากาศตามธีมด่าน (Sfx.Themes)

- `Core/Sfx.Themes.cs` สร้างเพลงต่อสู้ 10 สไตล์ (คอร์ด/จังหวะ/กลอง/เสียงลีดต่างกันต่อด่าน; Quick Battle ใช้เพลงต่อสู้ดั้งเดิม) และเสียงบรรยากาศวนลูป 8 วินาที: ป่า (ลม+นก), ทะเลทราย (ลม+กองไฟ), ซอยฝนตก, วิหาร (เสียงประสาน+ระฆัง), หอคอยเวท (ฮัมอาร์เคน+ประกาย), ลมหนาว, ภูเขาไฟ (ไฟปะทุ), ทะเล (คลื่น+นกนางนวล), คืนจันทร์ (ระฆังไกลๆ), จักรวาล
- ทุกอย่างสร้างจากโค้ด; วางไฟล์ `Resources/Music/battle_stage_05` (หรือ `ambience_stage_05`) เพื่อใช้ไฟล์จริงแทนของด่านนั้น. ระดับเสียงบรรยากาศใช้สไลเดอร์ Master x Effects, เพลงใช้ Master x Music
- ออกจากการต่อสู้ไปเมนู บรรยากาศจะค่อยๆ เบาลงแล้วหยุด

## Tutorial แบบโต้ตอบ

- ปุ่ม **Tutorial** ในเมนูหลัก (`CampaignService.StartTutorial`) ตั้ง `BattleSetup.Tutorial = true`: ผู้เล่นเริ่มก่อน ไม่มี Mulligan, การ์ดบนสุดของเด็คถูกจัดไว้ (`Battle/TutorialSetup.cs`), คู่ซ้อมมี HP 10 ลงได้แต่ยูนิต ไม่โจมตี ไม่ใช้สกิล (`AIPlanner.TutorialMode`)
- `Screens/TutorialCoach.cs` แสดงกล่องคำสอนมุมซ้ายบนและกรอบไฮไลต์กะพริบ เลื่อนขั้นเองเมื่อผู้เล่นทำตามจริง: เป้าหมาย → Energy → ลงยูนิต → จบเทิร์น → เทิร์นศัตรู → โจมตี → สเปล → เคล็ดลับ → ชนะ. กด Skip ข้ามได้
- ไม่นับสถิติ/Achievement/Daily/การ์ดลับ; ชนะครั้งแรกได้ Gold 100 และจำค่า `SaveData.TutorialDone` (ปุ่มจะไม่มี "(!)" อีก)

## เสียงเอฟเฟกต์ชุดเพิ่มเติม (Sfx.Effects)

`Core/Sfx.Effects.cs` เพิ่มเสียงสังเคราะห์ 20 แบบ (ซ้อนโทน + เสียงซ่าหลายชั้น): Attack, Death, Spell (ทั่วไป) และสเปลตามธาตุ Fire/Ice/Lightning/Dark/Water/Wind/Earth/Light
(เลือกอัตโนมัติจากแท็กการ์ดด้วย `Sfx.SpellSoundFor`), Draw, TurnStart, EndTurn, Buff, Error, Coin, Skill, Hover, Clang
ผูกกับหน้าต่อสู้: โจมตี/ตาย/ใช้สเปล/จั่วการ์ด/เริ่ม-จบเทิร์น/สกิล Leader/ลงยูนิต Taunt-Shield/คำสั่งไม่สำเร็จ/ชี้การ์ด และเมนู: รับรางวัล Daily-Milestone = Coin

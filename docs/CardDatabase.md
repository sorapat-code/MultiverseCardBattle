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
- ตัวอย่าง: `RFT_049 Excalibur` (Unit gear), `RFT_050 Holy Grail` (Leader gear)

## Keyword Backstab (เดิมชื่อ Poison)

ยูนิตที่โดนความเสียหายจากการ์ดที่มี `Backstab` จะถูกทำลาย (ใช้ใน CSV/ScriptableObject เป็น `Backstab` แทน `Poison`) ส่วนสถานะ `Poison` (ดาเมจต่อเนื่อง) ยังเป็นสถานะตามเดิม

## ชุดการ์ด (CardSet)

- `Core` (ซองละ 100 Gold) และ `Rift` (ซองละ 150 Gold) แยกกันที่ `CardData.Set`
- `Rift`: 50 ใบ (ยูนิต 30 / เวทมนตร์ 12 / อุปกรณ์ 8) ID `RFT_001`–`RFT_050` และ Token `TKN_007`–`TKN_013`
- ดูรายชื่อทั้งหมดได้ใน `docs/CardRework_Draft.md` และปุ่ม "View Card Pool" ในร้านค้า

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

## Per-character cutscenes (VergilScene)

`SecretCinematic` normally plays one shared timeline (only tint/epithet differ). Characters can now get
their own choreography: `SecretCinematic.Play` checks `data.CardID` and hands the backdrop/beats to a
dedicated scene class. First one: **Vergil** (`VergilScene.cs`, Unlock + Arrival modes; Devil Trigger
transform still uses the shared timeline) - "Judgment Cut": moon -> eye slit (card art zoomed, red/cyan
split) -> sheathe click -> accelerating slash cage -> freeze -> shatter + X -> card flips in.
All timings are constants/`Plan()` entries in `VergilScene.cs`. Not yet compiled/run in Unity.

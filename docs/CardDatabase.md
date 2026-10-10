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
- **Keyword**: `Taunt`, `Charge`, `Rush`, `Shield`, `Lifesteal`, `Poison`, `Stealth`, `DoubleStrike`, `Pierce`, `QuickAttack`, `Overwhelm`, `Unstoppable`, `Guard`, `Ward`, `Immune`, `Reborn`, `Spellburst`, `Shadowbound`
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
- Disenchant gives Dust (Common 5, Rare 20, Epic 100, Legendary 400). Crafting costs Dust
  (Common 40, Rare 100, Epic 400, Legendary 1600). Copies used by any deck are protected.
- Achievements live in `Data/AchievementService.cs`; stats are stored in the save file (version 3).
- A player whose deck reaches 0 cards loses the battle immediately.

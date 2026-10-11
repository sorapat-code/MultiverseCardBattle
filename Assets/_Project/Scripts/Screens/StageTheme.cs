using UnityEngine;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// ธีมภาพของโต๊ะ 3D แยกตามด่านใน Campaign (แสง หมอก สีพื้นสนาม/โต๊ะ ละอองในอากาศ โทนสีหลังประมวลผล)
    /// ด่านที่ไม่มีในรายการ / Quick Battle ใช้ธีมป่ายามสาย
    /// </summary>
    public class StageTheme
    {
        public string Name = "Forest";

        // แสง
        public Color Ambient = new Color(0.52f, 0.58f, 0.56f);
        public Color SunColor = new Color(1f, 0.94f, 0.82f);
        public float SunIntensity = 1.2f;
        public Vector3 SunEuler = new Vector3(52f, -28f, 0f);
        public Color Fog = new Color(0.07f, 0.13f, 0.13f);
        public float FogDensity = 0.011f;

        // พื้นผิว (คูณกับ texture)
        public Color BoardTint = Color.white;
        public Color TableTint = new Color(0.34f, 0.4f, 0.38f);
        public Color FoliageTint = Color.white;

        // อนุภาค
        public Color MoteA = new Color(1f, 0.9f, 0.45f);
        public Color MoteB = new Color(0.7f, 1f, 0.5f);
        public float MoteGravity = 0f;      // ลบ = ลอยขึ้น (ประกายไฟ) บวก = ร่วง (หิมะ)
        public float MoteRate = 5f;
        public Color DustColor = new Color(1f, 0.92f, 0.7f);
        public Color MistColor = new Color(0.5f, 0.78f, 0.85f);
        public float MistAlpha = 0.07f;
        public bool Leaves = true;
        public Color LeafA = new Color(0.45f, 0.75f, 0.3f);
        public Color LeafB = new Color(0.9f, 0.55f, 0.2f);

        // วงรูนกลางสนาม
        public Color Rune = new Color(1f, 0.78f, 0.35f);

        // โพสต์เอฟเฟกต์
        public float Saturation = 1.12f;
        public float Contrast = 1.04f;
        public float Exposure = 1.1f;
        public float Vignette = 0.3f;
        public Color LightTint = new Color(1.04f, 1f, 0.95f);
        public Color ShadowTint = new Color(0.93f, 0.98f, 1.03f);

        // ต้นไม้/หินรอบโต๊ะ: 0 = ปกติ, 1 = เบาบาง, 2 = ไม่มี
        public int Nature = 0;

        public static StageTheme Current
        {
            get
            {
                CampaignStage stage = BattleSetup.Stage;

                return For(stage != null ? stage.Id : null);
            }
        }

        public static StageTheme For(string stageId)
        {
            StageTheme t = new StageTheme();

            switch (stageId)
            {
                case "stage_02": // Bandit Road: ฝุ่นทองยามบ่ายแก่ ดินแห้ง
                    t.Name = "Bandit Road";
                    t.Ambient = new Color(0.6f, 0.54f, 0.46f);
                    t.SunColor = new Color(1f, 0.82f, 0.55f);
                    t.SunIntensity = 1.35f;
                    t.SunEuler = new Vector3(38f, -50f, 0f);
                    t.Fog = new Color(0.2f, 0.14f, 0.08f);
                    t.FogDensity = 0.012f;
                    t.BoardTint = new Color(1f, 0.82f, 0.5f);
                    t.TableTint = new Color(0.5f, 0.4f, 0.28f);
                    t.FoliageTint = new Color(1.1f, 0.95f, 0.6f);
                    t.MoteA = new Color(1f, 0.8f, 0.4f);
                    t.MoteB = new Color(1f, 0.65f, 0.3f);
                    t.DustColor = new Color(1f, 0.8f, 0.5f);
                    t.MistColor = new Color(0.85f, 0.65f, 0.4f);
                    t.MistAlpha = 0.06f;
                    t.LeafA = new Color(0.8f, 0.6f, 0.25f);
                    t.LeafB = new Color(0.65f, 0.4f, 0.15f);
                    t.Rune = new Color(1f, 0.6f, 0.25f);
                    t.LightTint = new Color(1.08f, 1f, 0.9f);
                    t.ShadowTint = new Color(1f, 0.94f, 0.9f);
                    break;

                case "stage_03": // Shadow Alley: กลางคืน ซอยหินเปียก นีออน
                    t.Name = "Shadow Alley";
                    t.Ambient = new Color(0.34f, 0.36f, 0.5f);
                    t.SunColor = new Color(0.55f, 0.6f, 1f);
                    t.SunIntensity = 0.9f;
                    t.SunEuler = new Vector3(60f, 20f, 0f);
                    t.Fog = new Color(0.06f, 0.05f, 0.12f);
                    t.FogDensity = 0.016f;
                    t.BoardTint = new Color(0.5f, 0.55f, 0.7f);
                    t.TableTint = new Color(0.3f, 0.3f, 0.42f);
                    t.FoliageTint = new Color(0.5f, 0.55f, 0.75f);
                    t.MoteA = new Color(1f, 0.4f, 0.8f);
                    t.MoteB = new Color(0.4f, 0.9f, 1f);
                    t.DustColor = new Color(0.7f, 0.7f, 1f);
                    t.MistColor = new Color(0.45f, 0.4f, 0.8f);
                    t.MistAlpha = 0.1f;
                    t.Leaves = false;
                    t.Rune = new Color(0.7f, 0.45f, 1f);
                    t.Saturation = 1.18f;
                    t.Exposure = 1.15f;
                    t.Vignette = 0.38f;
                    t.LightTint = new Color(0.98f, 0.98f, 1.06f);
                    t.ShadowTint = new Color(0.86f, 0.9f, 1.1f);
                    break;

                case "stage_04": // Temple Siege: หินทรายเรืองทอง แสงศักดิ์สิทธิ์
                    t.Name = "Temple Siege";
                    t.Ambient = new Color(0.62f, 0.58f, 0.5f);
                    t.SunColor = new Color(1f, 0.92f, 0.7f);
                    t.SunIntensity = 1.4f;
                    t.SunEuler = new Vector3(60f, -15f, 0f);
                    t.Fog = new Color(0.18f, 0.15f, 0.1f);
                    t.FogDensity = 0.009f;
                    t.BoardTint = new Color(0.95f, 0.92f, 0.72f);
                    t.TableTint = new Color(0.62f, 0.55f, 0.42f);
                    t.FoliageTint = new Color(1f, 0.95f, 0.75f);
                    t.MoteA = new Color(1f, 0.95f, 0.7f);
                    t.MoteB = new Color(1f, 1f, 0.9f);
                    t.MoteGravity = -0.02f;
                    t.DustColor = new Color(1f, 0.95f, 0.75f);
                    t.MistColor = new Color(1f, 0.9f, 0.65f);
                    t.MistAlpha = 0.05f;
                    t.Leaves = false;
                    t.Rune = new Color(1f, 0.9f, 0.5f);
                    t.Exposure = 1.15f;
                    t.LightTint = new Color(1.08f, 1.02f, 0.9f);
                    break;

                case "stage_05": // Mage Tower: ม่วงอาร์เคน ประกายเวทมนตร์
                    t.Name = "Mage Tower";
                    t.Ambient = new Color(0.44f, 0.4f, 0.62f);
                    t.SunColor = new Color(0.8f, 0.7f, 1f);
                    t.SunIntensity = 1.05f;
                    t.SunEuler = new Vector3(55f, 30f, 0f);
                    t.Fog = new Color(0.09f, 0.06f, 0.2f);
                    t.FogDensity = 0.013f;
                    t.BoardTint = new Color(0.62f, 0.6f, 1f);
                    t.TableTint = new Color(0.36f, 0.32f, 0.55f);
                    t.FoliageTint = new Color(0.6f, 0.55f, 1f);
                    t.MoteA = new Color(0.75f, 0.5f, 1f);
                    t.MoteB = new Color(0.4f, 0.8f, 1f);
                    t.MoteGravity = -0.015f;
                    t.MoteRate = 9f;
                    t.DustColor = new Color(0.8f, 0.7f, 1f);
                    t.MistColor = new Color(0.55f, 0.4f, 1f);
                    t.MistAlpha = 0.09f;
                    t.Leaves = false;
                    t.Rune = new Color(0.7f, 0.5f, 1f);
                    t.Saturation = 1.2f;
                    t.LightTint = new Color(1f, 0.98f, 1.08f);
                    t.ShadowTint = new Color(0.9f, 0.9f, 1.12f);
                    t.Nature = 1;
                    break;

                case "stage_06": // Titan Gate: หินเทาเย็น ลมหนาว หมอกหนา
                    t.Name = "Titan Gate";
                    t.Ambient = new Color(0.52f, 0.56f, 0.6f);
                    t.SunColor = new Color(0.85f, 0.92f, 1f);
                    t.SunIntensity = 1.1f;
                    t.SunEuler = new Vector3(48f, -35f, 0f);
                    t.Fog = new Color(0.14f, 0.16f, 0.19f);
                    t.FogDensity = 0.017f;
                    t.BoardTint = new Color(0.66f, 0.72f, 0.72f);
                    t.TableTint = new Color(0.46f, 0.48f, 0.5f);
                    t.FoliageTint = new Color(0.7f, 0.8f, 0.8f);
                    t.MoteA = new Color(0.9f, 0.95f, 1f);
                    t.MoteB = new Color(0.8f, 0.9f, 1f);
                    t.MoteGravity = 0.02f;
                    t.MoteRate = 8f;
                    t.DustColor = new Color(0.9f, 0.95f, 1f);
                    t.MistColor = new Color(0.75f, 0.85f, 0.95f);
                    t.MistAlpha = 0.11f;
                    t.Leaves = false;
                    t.Rune = new Color(0.65f, 0.85f, 1f);
                    t.Saturation = 0.95f;
                    t.Nature = 1;
                    break;

                case "stage_07": // Dragon Peak: ภูเขาไฟ แดงส้ม ประกายไฟลอยขึ้น
                    t.Name = "Dragon Peak";
                    t.Ambient = new Color(0.55f, 0.4f, 0.36f);
                    t.SunColor = new Color(1f, 0.6f, 0.35f);
                    t.SunIntensity = 1.3f;
                    t.SunEuler = new Vector3(35f, -60f, 0f);
                    t.Fog = new Color(0.22f, 0.08f, 0.05f);
                    t.FogDensity = 0.013f;
                    t.BoardTint = new Color(0.85f, 0.45f, 0.32f);
                    t.TableTint = new Color(0.3f, 0.2f, 0.18f);
                    t.FoliageTint = new Color(0.8f, 0.5f, 0.4f);
                    t.MoteA = new Color(1f, 0.55f, 0.15f);
                    t.MoteB = new Color(1f, 0.3f, 0.1f);
                    t.MoteGravity = -0.06f;
                    t.MoteRate = 12f;
                    t.DustColor = new Color(1f, 0.55f, 0.25f);
                    t.MistColor = new Color(0.9f, 0.3f, 0.15f);
                    t.MistAlpha = 0.08f;
                    t.Leaves = false;
                    t.Rune = new Color(1f, 0.4f, 0.15f);
                    t.Saturation = 1.2f;
                    t.LightTint = new Color(1.1f, 0.98f, 0.9f);
                    t.ShadowTint = new Color(1f, 0.9f, 0.9f);
                    t.Nature = 1;
                    break;

                case "stage_08": // Grand Line: ดาดฟ้าเรือ ทะเลสดใส ละอองน้ำ
                    t.Name = "Grand Line";
                    t.Ambient = new Color(0.58f, 0.64f, 0.7f);
                    t.SunColor = new Color(1f, 0.96f, 0.85f);
                    t.SunIntensity = 1.45f;
                    t.SunEuler = new Vector3(58f, -20f, 0f);
                    t.Fog = new Color(0.1f, 0.22f, 0.3f);
                    t.FogDensity = 0.008f;
                    t.BoardTint = new Color(0.88f, 0.62f, 0.38f);
                    t.TableTint = new Color(0.42f, 0.3f, 0.2f);
                    t.FoliageTint = new Color(0.9f, 1f, 0.9f);
                    t.MoteA = new Color(0.9f, 1f, 1f);
                    t.MoteB = new Color(0.7f, 0.95f, 1f);
                    t.MoteRate = 7f;
                    t.DustColor = new Color(1f, 1f, 1f);
                    t.MistColor = new Color(0.75f, 0.92f, 1f);
                    t.MistAlpha = 0.08f;
                    t.Leaves = false;
                    t.Rune = new Color(0.4f, 0.9f, 1f);
                    t.Saturation = 1.2f;
                    t.Exposure = 1.18f;
                    t.Nature = 2;
                    break;

                case "stage_09": // Soul Reaper's Trial: ค่ำคืนจันทร์ซีด กลีบซากุระร่วง
                    t.Name = "Soul Reaper's Trial";
                    t.Ambient = new Color(0.46f, 0.5f, 0.64f);
                    t.SunColor = new Color(0.8f, 0.86f, 1f);
                    t.SunIntensity = 1.05f;
                    t.SunEuler = new Vector3(50f, 40f, 0f);
                    t.Fog = new Color(0.07f, 0.09f, 0.18f);
                    t.FogDensity = 0.014f;
                    t.BoardTint = new Color(0.68f, 0.75f, 0.95f);
                    t.TableTint = new Color(0.36f, 0.4f, 0.55f);
                    t.FoliageTint = new Color(0.8f, 0.7f, 0.95f);
                    t.MoteA = new Color(0.8f, 0.9f, 1f);
                    t.MoteB = new Color(1f, 0.6f, 0.75f);
                    t.DustColor = new Color(0.85f, 0.9f, 1f);
                    t.MistColor = new Color(0.6f, 0.7f, 1f);
                    t.MistAlpha = 0.09f;
                    t.LeafA = new Color(1f, 0.7f, 0.82f);
                    t.LeafB = new Color(1f, 0.85f, 0.9f);
                    t.Rune = new Color(1f, 0.45f, 0.55f);
                    t.Saturation = 1.1f;
                    t.LightTint = new Color(1f, 0.98f, 1.06f);
                    t.ShadowTint = new Color(0.88f, 0.92f, 1.1f);
                    break;

                case "stage_10": // Multiverse Sovereign: จักรวาล ม่วงทองมายา
                    t.Name = "Multiverse";
                    t.Ambient = new Color(0.5f, 0.42f, 0.62f);
                    t.SunColor = new Color(1f, 0.85f, 1f);
                    t.SunIntensity = 1.1f;
                    t.SunEuler = new Vector3(45f, 0f, 0f);
                    t.Fog = new Color(0.1f, 0.04f, 0.18f);
                    t.FogDensity = 0.011f;
                    t.BoardTint = new Color(0.75f, 0.55f, 1f);
                    t.TableTint = new Color(0.34f, 0.24f, 0.48f);
                    t.MoteA = new Color(1f, 0.85f, 0.4f);
                    t.MoteB = new Color(0.9f, 0.4f, 1f);
                    t.MoteGravity = -0.02f;
                    t.MoteRate = 14f;
                    t.DustColor = new Color(1f, 0.85f, 1f);
                    t.MistColor = new Color(0.85f, 0.4f, 1f);
                    t.MistAlpha = 0.1f;
                    t.Leaves = false;
                    t.Rune = new Color(1f, 0.8f, 0.4f);
                    t.Saturation = 1.25f;
                    t.Contrast = 1.07f;
                    t.Vignette = 0.36f;
                    t.LightTint = new Color(1.05f, 0.98f, 1.06f);
                    t.ShadowTint = new Color(0.9f, 0.88f, 1.12f);
                    t.Nature = 2;
                    break;
            }

            return t;
        }
    }
}

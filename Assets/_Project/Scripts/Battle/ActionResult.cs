namespace MultiverseCardBattle.Battle
{
    /// <summary>
    /// ผลลัพธ์ของการสั่งงานในการต่อสู้ (สำเร็จ/ล้มเหลวพร้อมเหตุผล)
    /// </summary>
    public class ActionResult
    {
        public bool Success;
        public string Message;

        public static ActionResult Ok(string message = "")
        {
            return new ActionResult { Success = true, Message = message };
        }

        public static ActionResult Fail(string message)
        {
            return new ActionResult { Success = false, Message = message };
        }
    }
}

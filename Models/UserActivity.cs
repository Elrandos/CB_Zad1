namespace CB_Zad1.Models
{
    public class UserActivity
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
        public DateTime ActivityDate { get; set; } = DateTime.Now;
        public string IpAddress { get; set; }
    }
}

namespace BoroHFR.ViewModels.SysAdmin
{
    public class DeleteClassViewModel
    {
        public string Name { get; set; }
        public Guid Id { get; set; }
        public int UserCount { get; set; }
        public int EventCount { get; set; }
        public int ChatMessageCount { get; set; }
    }
}

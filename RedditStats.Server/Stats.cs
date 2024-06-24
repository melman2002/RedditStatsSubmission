namespace RedditStats.Server
{
	public class Stats
	{
		public IEnumerable<Post> Posts { get; set; } = new List<Post>();
		public IEnumerable<User> Users { get; set; } = new List<User>();
	}

	public class Post
	{
		public string Title { get; set; } = "";
		public string Username { get; set; } = "";
		public long Created { get; set; }
		public int UpVotesCount { get; set; }
		public string Url { get; set; } = "";
	}

	public class User
	{
		public string Name { get; set; } = "";
		public int PostsCount { get; set; }
	}
}

using Newtonsoft.Json;

namespace Slackord.Classes
{
    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string RealName { get; set; }
    }

    public class UserManager
    {
        private Dictionary<string, User> userLookup;

        public UserManager(string usersFilePath)
        {
            LoadUserData(usersFilePath);
        }

        private void LoadUserData(string filePath)
        {
            string jsonText = System.IO.File.ReadAllText(filePath);
            var users = JsonConvert.DeserializeObject<List<User>>(jsonText);
            userLookup = new Dictionary<string, User>();

            foreach (var user in users)
            {
                userLookup[user.Id] = user;
            }
        }

        public string GetUserName(string userId)
        {
            return userLookup.TryGetValue(userId, out var user) ? user.Name : userId;
        }
    }
}

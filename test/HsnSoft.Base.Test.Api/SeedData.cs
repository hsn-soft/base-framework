using HsnSoft.Base.Test.Api.Domain.Entities;

namespace HsnSoft.Base.Test.Api;

public static class SeedData
{
    public static List<User> GenerateUserData(int count)
    {
        string[] firstNames = ["Ali", "Ayşe", "Mehmet", "Fatma", "Ahmet", "Elif", "Hasan", "Zeynep", "Murat", "Emine"];
        string[] lastNames = ["Yılmaz", "Kara", "Demir", "Çelik", "Aydın", "Şahin", "Koç", "Arslan", "Güneş", "Kaplan"];

        var random = new Random();
        var users = new List<User>();

        for (int i = 0; i < count; i++)
        {
            string firstName = firstNames[random.Next(firstNames.Length)];
            string lastName = lastNames[random.Next(lastNames.Length)];
            int age = random.Next(18, 61); // 18–60 arası
            // bool isActive = random.Next(0, 2) == 1; // true/false
            // var created = DateTime.UtcNow.AddDays(-random.Next(0, 31)); // son 30 gün

            // email için duplicate olmasın diye i ekledim
            string email = $"{firstName.ToLower()}.{lastName.ToLower()}{i}@mail.com";

            users.Add(new User(Guid.NewGuid(), email, firstName, lastName, age ));
        }

        return users;
    }
}
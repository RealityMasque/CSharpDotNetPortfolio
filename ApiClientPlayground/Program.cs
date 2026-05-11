public class Program
{
    public static async Task Main()
    {
        var httpClient = new HttpClient();
        var apiClient = new ApiClient(httpClient);

        string token = await apiClient.PostLocalLogin("user");
        await apiClient.GetLocalProfile(token);
        await apiClient.GetLocalUser(token);
        await apiClient.GetLocalAdmin(token);

        token = await apiClient.PostLocalLogin("admin");
        await apiClient.GetLocalProfile(token);
        await apiClient.GetLocalUser(token);
        await apiClient.GetLocalAdmin(token);

        await apiClient.GetLocalSecret(token);
        await apiClient.LocalGetHello();
        await apiClient.LocalPostEcho("Hello, Echo!");

        await apiClient.GetPost();
        await apiClient.CreatePost();
        //await apiClient.GetUser();
        //await apiClient.PostLogin();
    }
}
using System.Text;
using System.Text.Json;

public class Program
{
    public static async Task Main()
    {
        string token = await PostLocalLogin();
        await GetLocalSecret(token);
        //await LocalGetHello();
        //await LocalPostEcho();
        //await GetPost();
        //await CreatePost();
        //await GetUser();
        //await PostLogin();
    }

    public static async Task GetPost()
    {
        string noAuthGetUrl = "https://jsonplaceholder.typicode.com/posts/1";
        Console.WriteLine($"Making GET request (no auth) to {noAuthGetUrl} in order to get a Post");
        try
        {
            using HttpClient client = new HttpClient();
            HttpResponseMessage noAuthResponse = await client.GetAsync(noAuthGetUrl);
            noAuthResponse.EnsureSuccessStatusCode();
            string jsonResponse = await noAuthResponse.Content.ReadAsStringAsync();
            Post? post = JsonSerializer.Deserialize<Post>(jsonResponse);
            
            Console.WriteLine($"GET Response (no auth) [Post object data]:");
            Console.WriteLine($"Post UserID: {post?.userId}");
            Console.WriteLine($"Post ID: {post?.id}");
            Console.WriteLine($"Post Title: {post?.title}");
            Console.WriteLine($"Post Body: {post?.body}");
            Console.WriteLine();
            Console.WriteLine();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"GET Request (no auth) to {noAuthGetUrl} failed: {ex.Message}");
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Other error from GET Request (no auth) to {noAuthGetUrl}: {ex.Message}");
        }
    }

    public static async Task CreatePost()
    {
        string noAuthPostUrl = "https://jsonplaceholder.typicode.com/posts";
        Console.WriteLine($"Making POST request (no auth) to {noAuthPostUrl} in order to create a new Post");
        try
        {
            using HttpClient client = new HttpClient();
            Post postData = new Post { title = "Hello API", body = "Testing POST", userId = 1 };
            string jsonPayload = JsonSerializer.Serialize(postData);
            StringContent content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage postResponse = await client.PostAsync(noAuthPostUrl, content);
            postResponse.EnsureSuccessStatusCode();
            string jsonResponse = await postResponse.Content.ReadAsStringAsync();

            Console.WriteLine($"POST Response (no auth) [json]:");
            Console.WriteLine(jsonResponse);
            Console.WriteLine();
            Console.WriteLine();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"POST Request (no auth) to {noAuthPostUrl} failed: {ex.Message}");
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Other error from POST Request (no auth) to {noAuthPostUrl}: {ex.Message}");
        }
    }

    public static async Task GetUser()
    {
        string reqResXApiKey = "reqres_27cb402f631e426d83ed7b79e3a232bb";
        string authGetUrl = "https://reqres.in/api/users/2";
        Console.WriteLine($"Making GET request (with auth) to {authGetUrl} in order to get a User");
        try
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-api-key", reqResXApiKey);
            HttpResponseMessage authResponse = await client.GetAsync(authGetUrl);
            authResponse.EnsureSuccessStatusCode();
            string jsonResponse = await authResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"GET Response (with auth) [json]:");
            Console.WriteLine(jsonResponse);
            Console.WriteLine();
            Console.WriteLine();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"GET Request (with auth) to {authGetUrl} failed: {ex.Message}");
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Other error from GET Request (with auth) to {authGetUrl}: {ex.Message}");
        }
    }

    public static async Task PostLogin()
    {
        string reqResXApiKey = "reqres_27cb402f631e426d83ed7b79e3a232bb";
        string authLoginUrl = "https://reqres.in/api/login";
        Console.WriteLine($"Making POST request (no auth) to {authLoginUrl} in order to login");
        try
        {
            var loginPayload = new { email = "eve.holt@reqres.in", password = "cityslicka" };
            string loginJson = JsonSerializer.Serialize(loginPayload);
            var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");

            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-api-key", reqResXApiKey);

            foreach(var header in client.DefaultRequestHeaders)
            {
                Console.WriteLine($"Header: {header.Key} = {string.Join(", ", header.Value)}");
            }

            HttpResponseMessage loginResponse = await client.PostAsync(authLoginUrl, loginContent);
            loginResponse.EnsureSuccessStatusCode();
            string jsonResponse = await loginResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"POST Response (with auth) [json]:");
            Console.WriteLine(jsonResponse);
            Console.WriteLine();
            Console.WriteLine();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"POST Request (with auth) to {authLoginUrl} failed: {ex.Message}");
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Other error from POST Request (with auth) to {authLoginUrl}: {ex.Message}");
        }
    }

    public static async Task LocalGetHello()
    {
        string localHelloUrl = "http://localhost:5159/hello";
        Console.WriteLine($"Making GET request to {localHelloUrl} in order to test hello endpoint");
        try
        {
            using HttpClient client = new HttpClient();
            HttpResponseMessage helloResponse = await client.GetAsync(localHelloUrl);
            helloResponse.EnsureSuccessStatusCode();
            string jsonResponse = await helloResponse.Content.ReadAsStringAsync();

            Console.WriteLine($"Local GET Response [json]:");
            Console.WriteLine(jsonResponse);
            Console.WriteLine();
            Console.WriteLine();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Local GET Request to {localHelloUrl} failed: {ex.Message}");
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Other error from local GET Request to {localHelloUrl}: {ex.Message}");
        }
    }

    public static async Task LocalPostEcho()
    {
        string localEchoUrl = "http://localhost:5159/echo";
        Console.WriteLine($"Making POST request to {localEchoUrl} in order to test echo endpoint");
        try
        {
            using HttpClient client = new HttpClient();
            var messagePayload = new { Content = "Hello, Echo!" };
            string messageJson = JsonSerializer.Serialize(messagePayload);
            var content = new StringContent(messageJson, Encoding.UTF8, "application/json");

            HttpResponseMessage echoResponse = await client.PostAsync(localEchoUrl, content);
            echoResponse.EnsureSuccessStatusCode();
            string jsonResponse = await echoResponse.Content.ReadAsStringAsync();

            Console.WriteLine($"Local POST Response [json]:");
            Console.WriteLine(jsonResponse);
            Console.WriteLine();
            Console.WriteLine();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Local POST Request to {localEchoUrl} failed: {ex.Message}");
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Other error from local POST Request to {localEchoUrl}: {ex.Message}");
        }
    }

    public static async Task<string> PostLocalLogin()
    {
        string localLoginUrl = "http://localhost:5159/login";
        Console.WriteLine($"Making POST request to {localLoginUrl} in order to test login endpoint");
        try
        {
            using HttpClient client = new HttpClient();
            var loginPayload = new { Username = "user", Password = "password" };
            string loginJson = JsonSerializer.Serialize(loginPayload);
            var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");

            var loginResponse = await client.PostAsync(localLoginUrl, loginContent);
            var loginResult = await loginResponse.Content.ReadAsStringAsync();
            var tokenObj = JsonSerializer.Deserialize<Dictionary<string, string>>(loginResult);
            if(tokenObj == null || !tokenObj.ContainsKey("token"))
            {
                Console.WriteLine($"Login response did not contain a token: {loginResult}");
                throw new Exception("Token not found in login response");
            }
            string token = tokenObj["token"];

            return token;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Local POST Request to {localLoginUrl} failed: {ex.Message}");
            throw;
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Other error from local POST Request to {localLoginUrl}: {ex.Message}");
            throw;
        }
    }

    public static async Task GetLocalSecret(string token)
    {
        string localSecretUrl = "http://localhost:5159/secret";
        Console.WriteLine($"Making GET request to {localSecretUrl} in order to test protected secret endpoint");
        try
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var secretResponse = await client.GetAsync(localSecretUrl);
            string jsonResponse = await secretResponse.Content.ReadAsStringAsync();

            Console.WriteLine($"Local GET Response [json]:");
            Console.WriteLine(jsonResponse);
            Console.WriteLine();
            Console.WriteLine();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Local GET Request to {localSecretUrl} failed: {ex.Message}");
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Other error from local GET Request to {localSecretUrl}: {ex.Message}");
        }
    }
}

public class Post
{
    public int userId { get; set; }
    public int id { get; set; }
    public string title { get; set; } = "";
    public string body { get; set; } = "";
}
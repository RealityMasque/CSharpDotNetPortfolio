using System.Text;
using System.Text.Json;

public class Program
{
    private static readonly HttpClient _httpClient = new HttpClient();

    public static async Task Main()
    {
        string token = await PostLocalLogin("user");
        await GetLocalProfile(token);
        await GetLocalUser(token);
        await GetLocalAdmin(token);
        
        token = await PostLocalLogin("admin");
        await GetLocalProfile(token);
        await GetLocalUser(token);
        await GetLocalAdmin(token);

        await GetLocalSecret(token);
        await LocalGetHello();
        await LocalPostEcho();

        await GetPost();
        await CreatePost();
        //await GetUser();
        //await PostLogin();
    }

    public static async Task GetPost()
    {
        string noAuthGetUrl = "https://jsonplaceholder.typicode.com/posts/1";
        Console.WriteLine($"Making GET request (no auth) to {noAuthGetUrl} in order to get a Post");
        try
        {
            HttpResponseMessage noAuthResponse = await _httpClient.GetAsync(noAuthGetUrl);
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
        catch(Exception ex)
        {
            Console.WriteLine($"Error from GET Request (no auth) to {noAuthGetUrl}: {ex.Message}");
        }
    }

    public static async Task CreatePost()
    {
        string noAuthPostUrl = "https://jsonplaceholder.typicode.com/posts";
        Console.WriteLine($"Making POST request (no auth) to {noAuthPostUrl} in order to create a new Post");
        try
        {
            Post postData = new Post { title = "Hello API", body = "Testing POST", userId = 1 };
            StringContent content = CreateJsonContent(postData);

            HttpResponseMessage postResponse = await _httpClient.PostAsync(noAuthPostUrl, content);
            postResponse.EnsureSuccessStatusCode();
            string jsonResponse = await postResponse.Content.ReadAsStringAsync();

            Console.WriteLine($"POST Response (no auth) [json]:");
            Console.WriteLine(jsonResponse);
            Console.WriteLine();
            Console.WriteLine();
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error from POST Request (no auth) to {noAuthPostUrl}: {ex.Message}");
        }
    }

    public static async Task GetUser()
    {
        string reqResXApiKey = "reqres_27cb402f631e426d83ed7b79e3a232bb";
        string authGetUrl = "https://reqres.in/api/users/2";
        Console.WriteLine($"Making GET request (with auth) to {authGetUrl} in order to get a User");
        try
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", reqResXApiKey);
            HttpResponseMessage authResponse = await _httpClient.GetAsync(authGetUrl);
            authResponse.EnsureSuccessStatusCode();
            string jsonResponse = await authResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"GET Response (with auth) [json]:");
            Console.WriteLine(jsonResponse);
            Console.WriteLine();
            Console.WriteLine();
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error from GET Request (with auth) to {authGetUrl}: {ex.Message}");
        }
    }

    public static async Task PostLogin()
    {
        string reqResXApiKey = "reqres_27cb402f631e426d83ed7b79e3a232bb";
        string authLoginUrl = "https://reqres.in/api/login";
        Console.WriteLine($"Making POST request (no auth) to {authLoginUrl} in order to login");
        try
        {
            var loginData = new { email = "eve.holt@reqres.in", password = "cityslicka" };
            StringContent content = CreateJsonContent(loginData);

            _httpClient.DefaultRequestHeaders.Add("x-api-key", reqResXApiKey);

            foreach(var header in _httpClient.DefaultRequestHeaders)
            {
                Console.WriteLine($"Header: {header.Key} = {string.Join(", ", header.Value)}");
            }

            HttpResponseMessage loginResponse = await _httpClient.PostAsync(authLoginUrl, content);
            loginResponse.EnsureSuccessStatusCode();
            string jsonResponse = await loginResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"POST Response (with auth) [json]:");
            Console.WriteLine(jsonResponse);
            Console.WriteLine();
            Console.WriteLine();
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error from POST Request (with auth) to {authLoginUrl}: {ex.Message}");
        }
    }

    public static async Task LocalGetHello()
    {
        string localHelloUrl = "http://localhost:5159/hello";
        Console.WriteLine($"Making GET request to {localHelloUrl} in order to test hello endpoint");
        try
        {
            HttpResponseMessage helloResponse = await _httpClient.GetAsync(localHelloUrl);
            helloResponse.EnsureSuccessStatusCode();
            string jsonResponse = await helloResponse.Content.ReadAsStringAsync();

            Console.WriteLine($"Local GET Response [json]:");
            Console.WriteLine(jsonResponse);
            Console.WriteLine();
            Console.WriteLine();
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error from local GET Request to {localHelloUrl}: {ex.Message}");
        }
    }

    public static async Task LocalPostEcho()
    {
        string localEchoUrl = "http://localhost:5159/echo";
        Console.WriteLine($"Making POST request to {localEchoUrl} in order to test echo endpoint");
        try
        {
            var messagePayload = new { Content = "Hello, Echo!" };
            StringContent content = CreateJsonContent(messagePayload);

            HttpResponseMessage echoResponse = await _httpClient.PostAsync(localEchoUrl, content);
            echoResponse.EnsureSuccessStatusCode();
            string jsonResponse = await echoResponse.Content.ReadAsStringAsync();

            Console.WriteLine($"Local POST Response [json]:");
            Console.WriteLine(jsonResponse);
            Console.WriteLine();
            Console.WriteLine();
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error from local POST Request to {localEchoUrl}: {ex.Message}");
        }
    }

    public static async Task<string> PostLocalLogin(string username = "user")
    {
        string localLoginUrl = "http://localhost:5159/login";
        Console.WriteLine($"Making POST request to {localLoginUrl} in order to test login endpoint");
        try
        {
            var loginPayload = new { Username = username, Password = "password" };
            StringContent content = CreateJsonContent(loginPayload);

            var loginResponse = await _httpClient.PostAsync(localLoginUrl, content);
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
        catch(Exception ex)
        {
            Console.WriteLine($"Error from local POST Request to {localLoginUrl}: {ex.Message}");
            throw;
        }
    }

    public static async Task GetLocalSecret(string token)
    {
        string localSecretUrl = "http://localhost:5159/secret";
        Console.WriteLine($"Making GET request to {localSecretUrl} in order to test protected secret endpoint");
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var secretResponse = await _httpClient.GetAsync(localSecretUrl);
            string jsonResponse = await secretResponse.Content.ReadAsStringAsync();

            Console.WriteLine($"Local GET Response [json]:");
            Console.WriteLine(jsonResponse);
            Console.WriteLine();
            Console.WriteLine();
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error from local GET Request to {localSecretUrl}: {ex.Message}");
        }
    }

    public static async Task GetLocalAdmin(string token)
    {
        string localAdminUrl = "http://localhost:5159/admin";
        Console.WriteLine($"Making GET request to {localAdminUrl} in order to test protected admin endpoint");
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var adminResponse = await _httpClient.GetAsync(localAdminUrl);
            string jsonResponse = await adminResponse.Content.ReadAsStringAsync();

            Console.WriteLine($"Local GET Response [json]:");
            Console.WriteLine(adminResponse.StatusCode.ToString());
            Console.WriteLine($"{(int)adminResponse.StatusCode} {adminResponse.StatusCode}");
            Console.WriteLine(jsonResponse);
            Console.WriteLine();
            Console.WriteLine();
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error from local GET Request to {localAdminUrl}: {ex.Message}");
        }
    }

    public static async Task GetLocalUser(string token)
    {
        string localUserUrl = "http://localhost:5159/user";
        Console.WriteLine($"Making GET request to {localUserUrl} in order to test protected user endpoint");
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var userResponse = await _httpClient.GetAsync(localUserUrl);
            string jsonResponse = await userResponse.Content.ReadAsStringAsync();

            Console.WriteLine($"Local GET Response [json]:");
            Console.WriteLine($"{(int)userResponse.StatusCode} {userResponse.StatusCode}");
            Console.WriteLine(jsonResponse);
            Console.WriteLine();
            Console.WriteLine();
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error from local GET Request to {localUserUrl}: {ex.Message}");
        }
    }

    public static async Task GetLocalProfile(string token)
    {
        string localProfileUrl = "http://localhost:5159/profile";
        Console.WriteLine($"Making GET request to {localProfileUrl} in order to test protected profile endpoint");
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var profileResponse = await _httpClient.GetAsync(localProfileUrl);
            string jsonResponse = await profileResponse.Content.ReadAsStringAsync();

            Console.WriteLine($"Local GET Response [json]:");
            Console.WriteLine($"{(int)profileResponse.StatusCode} {profileResponse.StatusCode}");
            Console.WriteLine(jsonResponse);
            Console.WriteLine();
            Console.WriteLine();
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error from local GET Request to {localProfileUrl}: {ex.Message}");
        }
    }

    private static StringContent CreateJsonContent(object data)
    {
        string jsonPayload = JsonSerializer.Serialize(data);
        return new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
    }
}

public class Post
{
    public int userId { get; set; }
    public int id { get; set; }
    public string title { get; set; } = "";
    public string body { get; set; } = "";
}
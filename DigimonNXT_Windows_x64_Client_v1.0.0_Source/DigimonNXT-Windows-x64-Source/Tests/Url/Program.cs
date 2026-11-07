using DigimonNXT.Client;
int checks=0, failures=0;
void Equal(string input,string expected){checks++;try{var actual=ServerAddress.Parse(input).AbsoluteUri;if(actual!=expected){failures++;Console.WriteLine($"FAIL normalize {input} expected={expected} actual={actual}");}}catch(Exception error){failures++;Console.WriteLine($"FAIL normalize {input} error={error.Message}");}}
void Reject(string input){checks++;try{var actual=ServerAddress.Parse(input);failures++;Console.WriteLine($"FAIL rejected input accepted {input} -> {actual}");}catch(ArgumentException){}catch(UriFormatException){}}
void Owns(string source,string target,bool expected){checks++;try{var actual=ServerAddress.InGame(ServerAddress.Parse(source),new Uri(target));if(actual!=expected){failures++;Console.WriteLine($"FAIL ownership {source} target={target}");}}catch(Exception error){failures++;Console.WriteLine($"FAIL ownership {error.Message}");}}
Equal("127.0.0.1","http://127.0.0.1/DigimonNXT/");
Equal("http://127.0.0.1","http://127.0.0.1/DigimonNXT/");
Equal("https://play.example.com","https://play.example.com/DigimonNXT/");
Equal("play.example.com/","https://play.example.com/");
Equal("127.0.0.1/","http://127.0.0.1/");
Equal("192.168.1.25:8080","http://192.168.1.25:8080/DigimonNXT/");
Equal("localhost:8080/game","http://localhost:8080/game/");
Equal("play.example.com","https://play.example.com/DigimonNXT/");
Equal("https://PLAY.EXAMPLE.COM/game","https://play.example.com/game/");
Equal("https://play.example.com/","https://play.example.com/");
Equal("http://[::1]:8080/DigimonNXT","http://[::1]:8080/DigimonNXT/");
Equal("http://192.168.1.25/DigimonNXT/index.php","http://192.168.1.25/DigimonNXT/");
Reject("");Reject("ftp://example.com/");Reject("http://user:pass@example.com/");Reject("https://example.com/game?x=1");Reject("https://example.com/game#x");
Reject("http://example.com:0/game");Reject("http://example.com:65536/game");
Reject("http://example.com/a/../game");Reject("http://example.com/a/./game");Reject("http://example.com/a/.");
Reject("http://example.com/a/%2e%2e/game");Reject("http://example.com/a/%5cgame");Reject("http://example.com/a\\game");
Owns("http://example.com/game/","http://example.com/game/assets/a.png",true);
Owns("http://example.com/game/","http://other.example.com/game/assets/a.png",false);
Owns("http://example.com/game/","https://example.com/game/assets/a.png",false);
Owns("http://example.com/game/","http://example.com:81/game/assets/a.png",false);
Owns("http://example.com/game/","http://example.com/games/assets/a.png",false);
Owns("http://example.com/game/","http://example.com:80/game/assets/a.png",true);
Console.WriteLine($"Windows URL checks: {checks}, failures: {failures}");
return failures==0?0:1;

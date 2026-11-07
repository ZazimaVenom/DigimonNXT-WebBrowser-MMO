using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DigimonNXT.Client;

int checks=0,failures=0;
void Check(bool pass,string description){checks++;if(!pass){failures++;Console.WriteLine("FAIL "+description);}}
string Hash(byte[] value)=>Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();
byte[] bundled=Encoding.UTF8.GetBytes("bundled png bytes"),updated=Encoding.UTF8.GetBytes("updated png bytes"),broken=Encoding.UTF8.GetBytes("broken  png bytes");
AssetManifest Manifest(byte[] bytes)=>new(){Protocol=1,Game="Digimon NXT",Assets=new(){["assets/fixture.png"]=new(){Sha256=Hash(bytes),Size=bytes.Length,Mime="image/png"}}};
var gameAssets=Path.Combine(AppContext.BaseDirectory,"GameAssets","assets");Directory.CreateDirectory(gameAssets);
await File.WriteAllBytesAsync(Path.Combine(gameAssets,"fixture.png"),bundled);
await File.WriteAllTextAsync(Path.Combine(AppContext.BaseDirectory,"asset-manifest.json"),JsonSerializer.Serialize(Manifest(bundled)));
var socket=new TcpListener(IPAddress.Loopback,0);socket.Start();int port=((IPEndPoint)socket.LocalEndpoint).Port;socket.Stop();
using var listener=new HttpListener();listener.Prefixes.Add($"http://127.0.0.1:{port}/");listener.Start();
AssetManifest remote=Manifest(bundled);byte[] served=updated;int status=200,assetRequests=0;string etag="\"fixture-bundle\"";string? requestedEtag=null;
using var stop=new CancellationTokenSource();
var host=Task.Run(async()=>{
 while(!stop.IsCancellationRequested){
  HttpListenerContext context;try{context=await listener.GetContextAsync();}catch{break;}
  try{
   byte[] body;
   if(context.Request.Url!.AbsolutePath.EndsWith("/_client_manifest.php")){context.Response.StatusCode=status;requestedEtag=context.Request.Headers["If-None-Match"];context.Response.Headers["ETag"]=etag;body=status==304?Array.Empty<byte>():JsonSerializer.SerializeToUtf8Bytes(remote);context.Response.ContentType="application/json";}
   else{assetRequests++;body=served;context.Response.ContentType="image/png";}
   context.Response.ContentLength64=body.Length;await context.Response.OutputStream.WriteAsync(body);context.Response.Close();
  }catch{context.Response.Abort();}
 }
});
var server=new Uri($"http://127.0.0.1:{port}/game/");var resource=new Uri(server,"assets/fixture.png");
using(var cache=new AssetCache(server)){
 await cache.RefreshAsync(CancellationToken.None,true);
 Check(cache.ManifestAvailable&&cache.MatchingCount==1,"matching remote manifest accepted");
 var initial=await cache.GetAsync(resource,CancellationToken.None);
 Check(initial!=null&&initial.Bytes.SequenceEqual(bundled)&&assetRequests==0,"matching bundle served without asset download");
 status=304;await cache.RefreshAsync(CancellationToken.None,true);
 Check(requestedEtag==etag,"confirmed server ETag sent with If-None-Match on refresh");
 var notModified=await cache.GetAsync(resource,CancellationToken.None);
 Check(cache.ManifestAvailable&&cache.MatchingCount==1&&notModified!=null&&notModified.Bytes.SequenceEqual(bundled)&&assetRequests==0,"304 keeps verified manifest and bundled asset without downloading either again");
 status=200;etag="\"fixture-updated\"";
 Check(!cache.TryAsset(new Uri($"http://127.0.0.1:{port+1}/game/assets/fixture.png"),out _),"other origin port excluded");
 Check(!cache.TryAsset(new Uri($"https://127.0.0.1:{port}/game/assets/fixture.png"),out _),"other origin scheme excluded");
 Check(!cache.TryAsset(new Uri(server,"../other/assets/fixture.png"),out _),"other base path excluded");
 Check(!cache.TryAsset(new Uri(server,"login.php"),out _),"game pages excluded");
 remote=Manifest(updated);await cache.RefreshAsync(CancellationToken.None,true);
 var changed=await cache.GetAsync(resource,CancellationToken.None);
 Check(changed!=null&&changed.Bytes.SequenceEqual(updated)&&assetRequests==1,"changed hash downloads requested replacement once");
 var repeat=await cache.GetAsync(resource,CancellationToken.None);
 Check(repeat!=null&&repeat.Bytes.SequenceEqual(updated)&&assetRequests==1,"downloaded matching update reused");
 remote=Manifest(broken);await cache.RefreshAsync(CancellationToken.None,true);
 var mismatch=await cache.GetAsync(resource,CancellationToken.None);
 Check(mismatch==null,"incorrect downloaded digest falls through to browser");
 remote=new(){Protocol=1,Game="Digimon NXT"};await cache.RefreshAsync(CancellationToken.None,true);
 Check(await cache.GetAsync(resource,CancellationToken.None)==null,"removed resource no longer intercepted");
 remote=Manifest(bundled);status=503;await cache.RefreshAsync(CancellationToken.None,true);
 Check(!cache.ManifestAvailable&&await cache.GetAsync(resource,CancellationToken.None)==null,"failed manifest probe disables stale local override");
 status=200;remote=Manifest(updated);
}
int previous=assetRequests;
var otherServer=new Uri($"http://127.0.0.1:{port}/other/");
using(var other=new AssetCache(otherServer)){
 await other.RefreshAsync(CancellationToken.None,true);
 var response=await other.GetAsync(new Uri(otherServer,"assets/fixture.png"),CancellationToken.None);
 Check(response!=null&&response.Bytes.SequenceEqual(updated)&&assetRequests==previous+1,"cache files isolated by selected server and base path");
}
stop.Cancel();listener.Close();await host;
foreach(var selected in new[]{server,otherServer}){var directory=Path.Combine(ClientSettings.DataDirectory,"AssetCache",ClientSettings.ProfileKey(selected));if(Directory.Exists(directory))Directory.Delete(directory,true);}
Console.WriteLine($"Windows actual asset cache checks: {checks}, failures: {failures}");
return failures==0?0:1;

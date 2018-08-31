A score-keeping service for games written as an ASP.NET Core Web API, with endpoints for adding, retrieving, and deleting scores.

This project uses a custom abstraction mechanism to simplify access to a MySQL database. To access a database, an interface can be created, and the methods can be annotated with the `SqlQuery` or `SqlUpdate` attributes to specify the SQL commands they should execute. The method parameters are automatically mapped to query parameters, and query results are automatically mapped to return values.

Example:

```csharp
public class HighScore
{
    public string name;
    public int score;
}

public interface HighScoreService
{
    [SqlQuery("select `name`, `score` from `scores` where `game`=@game order by `score` desc limit @max")]
    List<HighScore> GetScores(string game, int max);
}

// `ProxyFactory` generates an implementation of `HighScoreService` at runtime
// which forwards all method calls to `SqlProxyTarget`, which executes the
// appropriate SQL commands based on the `SqlQuery` or `SqlUpdate` attributes
// attached to each interface method.

var proxyTarget = new SqlProxyTarget("MySQL connection string");
var highScoreService = ProxyFactory.Create<HighScoreService>(proxyTarget);
List<HighScore> scores = highScoreService.GetScores("my game", 100);
PrintAllScores(scores);
```

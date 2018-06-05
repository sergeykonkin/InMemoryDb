[![AppVeyor](https://img.shields.io/appveyor/ci/sergeykonkin/inmemorydb.svg?style=flat-square)](https://ci.appveyor.com/project/sergeykonkin/inmemorydb)
[![AppVeyor tests](https://img.shields.io/appveyor/tests/sergeykonkin/inmemorydb.svg?style=flat-square)](https://ci.appveyor.com/project/sergeykonkin/inmemorydb/build/tests)
[![NuGet](https://img.shields.io/nuget/v/InMemoryDb.svg?style=flat-square)](https://www.nuget.org/packages/InMemoryDb/)

# Your DB fits in RAM

This library allows to store remote data in RAM and keep it updated indefinitely. `IContinuousReader` type performs continuous reading and `InMemoryReplica` uses it and stores data in strong-typed collection.

## Usage

For now there is only SQL Server implementation.

Table:
```sql
CREATE TABLE [dbo].[User] (
    [Id]        INT            IDENTITY (1, 1) NOT NULL,
    [FirstName] NVARCHAR (250) NULL,
    [LastName]  NVARCHAR (250) NULL,
    [Age]       INT            NULL,
    [Gender]    BIT            NULL
);
```
Model:
```csharp
public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int Age { get; set; }
    public bool Gender { get; set; }
}
```
Usage:
```csharp
var reader = new ContinuousReader<User>(new SqlBatchReader<User>(connectionString));
var replica = new InMemoryReplica<int, User>(reader);
reader.Start();
await reader.WhenInitialReadFinished(); // or -> await replica.WhenInitialReadFinished();

// InMemoryReplica<TKey, TValue> implements IReadOnlyDictionary<TKey, TValue>:
var count = replica.Count;
if (replica.ContainsKey(5))
{
    var user = replica[5];
}
...
```

This will create in-memory replica of origin table and will keep it updated.
### Under the hood
Reader will start reading using following script:
```sql
SELECT TOP (1000)
    *
FROM [User]
WHERE [Id] > @rowKey
ORDER BY [Id] ASC
```
with `@rowKey` = 0.
Then it will increment `@rowKey` with the max value of `Id` in the current batch. After whole data is read it will keep polling database with 200ms delay (configurable through constructor).

### Reading by timestamps

The bad part in the first example is it will only read new data on inserts, but not on updates. To overcome this, ROWVERSION (or TIMESTAMP) table column can be used:

Add timestamp column:
```sql
ALTER TABLE [User]
    ADD [_ts] ROWVERSION NOT NULL
```
Usage:
```csharp
var reader = new SqlContinuousTimestampReader<User>(connectionString);
var table = new InMemoryReplica<int, User>(reader, user => user.Id);
...
```
This approach will read data depending on current value of `[_ts]` column (which is updated on each insert/update) and build in-memory collection with keys of user IDs.

### Configuration

1. Custom table tame

```csharp
[Table("custom_table_name")]
public class User
{
...
```

2. Custom column name
```csharp
[Column("custom_column_name")]
public string FirstName { get; set; }
```

3. Custom row key
```csharp
[RowKey]
public int Foo { get; set; }
```

4. Ignore property
```csharp
[Ignore]
public string FullName => $"{FirstName} {LastName}";
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details
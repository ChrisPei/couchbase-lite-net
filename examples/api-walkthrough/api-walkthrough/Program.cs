using System;
using System.IO;
using Couchbase.Lite;
using Couchbase.Lite.Query;

namespace api_walkthrough
{
    class Program
    {
        static void Main(string[] args)
        {
            // create database
            var database = new Database("my-database");

            // create document
            var newTask = new Document();
            newTask.Set("type", "task");
            newTask.Set("owner", "todo");
            newTask.Set("createdAt", DateTimeOffset.UtcNow);
            database.Save(newTask);

            // mutate document
            newTask.Set("name", "Apples");
            database.Save(newTask);

            // typed accessors
            newTask.Set("createdAt", DateTimeOffset.UtcNow);
            var date = newTask.GetDate("createdAt");

            // database transaction
            database.InBatch(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    var doc = new Document();
                    doc.Set("type", "user");
                    doc.Set("name", $"user {i}");
                    database.Save(doc);
                    Console.WriteLine($"saved user document {doc.GetString("name")}");
                }
            });

            // blob
            var bytes = File.ReadAllBytes("avatar.jpg");
            var blob = new Blob("image/jpg", bytes);
            newTask.Set("avatar", blob);
            database.Save(newTask);
            var taskBlob = newTask.GetBlob("avatar");
            var data = taskBlob.Content;

            // query
            var query = QueryFactory.Select()
            .From(DataSourceFactory.Database(database))
            .Where(ExpressionFactory.Property("type").EqualTo("user")
		   .And(ExpressionFactory.Property("admin").EqualTo(false)));

            var rows = query.Run();
            foreach (var row in rows)
            {
                Console.WriteLine($"doc ID :: ${row.DocumentID}");
            }

            // fts example
            // insert documents
            var tasks = new string[] { "buy groceries", "play chess", "book travels", "buy museum tickets" };
            foreach (string task in tasks)
            {
                var doc = new Document();
                doc.Set("type", "task").Set("name", task); // Chaining is possible
                database.Save(doc);
            }

            // create Index
            database.CreateIndex(new[] { "name" }, IndexType.FullTextIndex, null);

            var ftsQuery = QueryFactory.Select()
		    .From(DataSourceFactory.Database(database))
		    .Where(ExpressionFactory.Property("name").Match("'buy'"));

            var ftsRows = ftsQuery.Run();
            foreach (var row in ftsRows)
            {
                Console.WriteLine($"document properties {row.Document.ToDictionary()}");
            }

            // replication
            var url = new Uri("blip://localhost:4984/db");
            var replication = database.CreateReplication(url);
            replication.Start();
        }
    }
}

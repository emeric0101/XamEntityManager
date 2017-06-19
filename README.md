# XamEntityManager

Xamarin client service for PHPAngular server https://github.com/emeric0101/PHPAngular
It includes a dependency injection.

## Installation

1. Add the package tu nuget

2. Implement the interface : ISQLite.cs : (you need to install sqlite.net https://github.com/oysteinkrog/SQLite.Net-PCL for each plateform)

```C#
    public interface ISQLite
    {
        SQLiteConnection GetConnection();
    }
```
3. Implement the user interface, the user class must derive from XamEntityManager.Entity.AEntity
```C#
    public interface IUser
    {
        string Sid { get; set; }
    }
 ```


4. In your App.cs, you have to create the Depedency Injector : (User is the implementation of IUser)

```C#
diService = new DiService("https://REST_URL/", typeof(User));
```


## Usage

### DI 

To use the depedency injector, you have to use it each time you create a page : 
```
            app.MainPage = diService.createPage(typeof(DmoApp.Login));
```
In each class which is injected, you can provided an array to inject other classes : 
```C#
		public static Type[] inject =  {
            typeof(MY OTHER CLASS SERVICE 1),
            typeof(MY OTHER CLASS SERVICE 2)
        };
        // then in the constructor : 
    public MyClass(MY OTHER CLASS SERVICE 1 myObj1, MY OTHER CLASS SERVICE 2 myObj2)
    {
    
    }
```

### Repository service

Each data model you need must be declared with a class which derived from XamEntityManager.Entity.AEntity.
To get data from the server, you have some method from the repository service.
First, don't forget to inject it (typeof(XamEntityManager.Service.RepositoryService)
Then, you can use from the repository object :  (like phpangular server side)
- T obj = await findById<T>(int id)
- List<T> objs = await findAll<T>
- List<T> objs = await findSome<T>(string method, int id, Dictionary<string, dynamic> args)

### EntityManager

When you have create a model and you want to save it, you use the EntityManager
First, inject it typeof(XamEntityManager.Service.EntityManager)
Then, use it to persist the model : 
```C#
  entityManager.persist(MY MODEL);
  await entityManager.flush(); // Synchronise the persist cache to the server
 ```
 
 ### User login
 To login a user, inject LoginService then 
 ```C#
 bool result = await login<T>(string mail, string password, bool stay)
 await logout() // to logout...
 IUser user = await getUser() // get the logged user (or null if not logged)
 ```
 
 Enjoy !
 

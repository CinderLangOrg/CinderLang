<p align="center">
  <img width="30% alt="Logo" src="art/LogoFull.png" />
</p>

<p align="center">
  <img alt="GitHub forks" src="https://img.shields.io/github/forks/CinderLangOrg/CinderLang?style=for-the-badge&labelColor=355FAD&color=058EFF">
  <img alt="GitHub License" src="https://img.shields.io/github/license/CinderLangOrg/CinderLang?style=for-the-badge&labelColor=355FAD&color=058EFF">
  <img alt="GitHub Repo stars" src="https://img.shields.io/github/stars/CinderLangOrg/CinderLang?style=for-the-badge&labelColor=355FAD&color=058EFF">
</p>

<p align="center">
  CInder is an unmanaged C-inspired programming language.
</p>

# Syntax
### Namespace

Cinder namespaces behave a lot like go modules. Namepsaces cannot be nested tho can be separated by periods to create categories

```csharp
namespace MyNs
{
  // insert code here....
}

namespace MyCategory.MyNs2
{
  // insert code here....
}
```

### Functions

In Cinder functions are defined using the `def` folowed by the name and an additional type (if the type is omitted, the default is `void`). 
Non `void` functions must have a return

```csharp
namespace ....
{
  def Test1()
  {
    // insert code here....
  }

  def Test2() : int
  {
    // insert code here....
    return 0;
  }
}
```

### Variables

Cinder is a typed language, Which means that all variables must define a type.
In Cinder variables can be both global and local.

```csharp
namespace ....
{
  int GLobal = 0;

  def Test1()
  {
    int local = 5;
    int local2;
  }
}
```

# Packages
&emsp; <img alt="NuGet Version" src="https://img.shields.io/nuget/vpre/cinderlang.backend.interface?style=for-the-badge&label=cinderlang.backend.interface&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2Fcinderlang.backend.interface%2F">

# Building

### Requirements
- dotnet 9.0

1) clone the repository by running `git clone https://github.com/CinderLangOrg/CinderLang.git --recursvie`.

### Visual studio
2) Open the solution
3) Hit the build button

### Command line
2) `cd` into the solution directory
3) run `dotnet build CinderLang.sln`

# Contributing

All additional backends have to be made as separate repositories, containing only the backend project.

<a href="https://www.star-history.com/?repos=CinderLangOrg%2FCinderLang&type=date&legend=bottom-right">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/image?repos=CinderLangOrg/CinderLang&type=date&theme=dark&legend=bottom-right" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/image?repos=CinderLangOrg/CinderLang&type=date&legend=bottom-right" />
   <img alt="Star History Chart" src="https://api.star-history.com/image?repos=CinderLangOrg/CinderLang&type=date&legend=bottom-right" />
 </picture>
</a>

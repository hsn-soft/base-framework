using AutoMapper;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Services;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Test.Api.Application.Contracts;
using HsnSoft.Base.Test.Api.Domain.Consts;
using HsnSoft.Base.Test.Api.Domain.Entities;
using HsnSoft.Base.Test.Api.Domain.Repositories;

namespace HsnSoft.Base.Test.Api.Application.Services;

public class EfCoreUserService(IEfCoreUserRepository userRepository, IUnitOfWork unitOfWork, IMapper mapper) : IEfCoreUserService
{
    public async Task<UserDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken: cancellationToken);
        return mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> FindFirstUserAsync(GetOrderedUserFilterDto input, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetFirstOrDefaultAsync(
            x => x.FirstName == input.FirstName,
            selector: s => new UserDto { Id = s.Id, FullName = s.FirstName + " " + s.LastName, Email = s.Email },
            includeEntity: null,
            o => o.OrderByDescending(n => n.Email),
            cancellationToken);

        return user;
    }

    public async Task<List<UserDto>> GetUserListAsync(GetOrderedUserFilterDto input, CancellationToken cancellationToken = default)
    {
        return await userRepository.GetListWithMapperAsync<UserDto>(new ListQueryOptions<User>(), mapper.ConfigurationProvider, cancellationToken: cancellationToken);

        //     var pageResult = await _userRepository.GetPageListAsync(
        //         selector: u => new UserDto { Id = u.Id, FullName = u.FirstName + " " + u.LastName, Email = u.Email },
        //         options: new PageQueryOptions<User>
        //         {
        //             Filter = x => x.IsActive,
        //             OrderByEntity = q => q.OrderByDescending(u => u.LastName).ThenBy(u => u.FirstName),
        //             OrderByDynamic = "LastName desc, FirstName asc", // Multi-column order
        //             PageNumber = 2,
        //             PageSize = 10
        //         }
        //     );
        //
        //     Console.WriteLine($"Toplam Kayıt: {pageResult.TotalCount}");
        //     Console.WriteLine($"Sayfa {pageResult.PageNumber}/{Math.Ceiling((double)pageResult.TotalCount / pageResult.PageSize)}");
        //
        //     var activeUsers = await _userRepository.GetPageListAsync(
        //         new PageQueryOptions<User> { Filter = u => u.IsActive }
        //     );
        //
        //     Console.WriteLine($"Toplam Kayıt: {activeUsers.TotalCount}");
        //     Console.WriteLine($"Sayfa {activeUsers.PageNumber}/{Math.Ceiling((double)activeUsers.TotalCount / activeUsers.PageSize)}");
        //
        //     //   var projectedQuery = await _userRepository.GetProjectedPageListAsync(
        //     //       new QueryOptions<User>
        //     //       {
        //     //           Filter = u => u.IsActive,
        //     //           OrderBy = q => q.OrderBy(u => u.LastName),
        //     //           PageNumber = 2,
        //     //           PageSize = 10
        //     //       }
        //     //   );
        //     //
        //     //
        //     // var  projectedResult=  await projectedQuery
        //     //      .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
        //     //      .ToListAsync();
        //     //
        //     //   return new PagedResult<UserDto>
        //     //   {
        //     //       Items = projectedResult,
        //     //       TotalCount = totalCount,
        //     //       PageNumber = options.PageNumber ?? 1,
        //     //       PageSize = options.PageSize ?? totalCount
        //     //   };
        //
        //
        //     // 1. IQueryable Dönmek ve Projection Yapmak
        //     // Avantaj: EF Core SQL’e sadece Id, FirstName, LastName, Email kolonlarını seçer.
        //     // Dezavantaj: Repository dışarıya IQueryable açtığı için biraz daha esneklik kaybı var (ama CQRS pattern ile yönetilebilir).
        //
        //     // return _userRepository.Query()
        //     //      .Select(u => new UserDto { Id = u.Id, FullName = u.FirstName + " " + u.LastName, Email = u.Email })
        //     //      .AsEnumerable();
        //
        //     // (best) 2.AutoMapper ProjectTo Kullanmak
        //     //Burada EF Core SQL seviyesinde sadece DTO’daki alanları çeker.
        //
        //     return await _userRepository.Query()
        //         .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
        //         .ToListAsync();
        //
        //     // 3. Expression Parametresi ile Dinamik Projection
        //     // Bu yaklaşımda gereksiz kolon çekilmez, Application katmanı tam kontrol sağlar.
        //     // Dezavantajı: Her seferinde selector yazman gerekir.
        //
        //     // return  await _userRepository.GetAsync(u => new UserDto
        //     //  {
        //     //      Id = u.Id,
        //     //      FullName = u.FirstName + " " + u.LastName
        //     //  });
        //
        //     var users = await _userRepository.GetAllAsync();
        //     return _mapper.Map<IEnumerable<UserDto>>(users);
    }

    public Task<List<UserSearchResultDto>> SearchUserListAsync(SearchOrderedFilterDto input, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<long> GetUserCountAsync(GetUserFilterDto input, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<PagedQueryResult<UserDto>> GetPagingUsersAsync(GetPagingUserFilterDto input, CancellationToken cancellationToken = default)
    {
        input ??= new GetPagingUserFilterDto();
        if (input.MaxAge.HasValue) input.MaxAge = input.MaxAge.Value + 1;

        var filter = new FilterBuilder<User>()
            .And(!string.IsNullOrWhiteSpace(input.FirstName) ? u => u.FirstName.Contains(input.FirstName) : null)
            .And(!string.IsNullOrWhiteSpace(input.LastName) ? u => u.LastName.Contains(input.LastName) : null)
            .And(input.MinAge is > 0 ? u => u.Age >= input.MinAge.Value : null)
            .And(input.MaxAge is > 0 ? u => u.Age < input.MaxAge.Value : null)
            .Build();

        var result = await userRepository.GetPageListAsync(
            selector: u => new UserDto { Id = u.Id, FullName = u.FirstName + " " + u.LastName, Email = u.Email },
            options: new PagedQueryOptions<User>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(input.OrderByText)
                    ? UserConsts.GetDefaultSorting()
                    : input.OrderByText,
                PageNumber = input.PageNumber ?? 1,
                MaxResultCount = input.PageSize ?? 10
            }, cancellationToken: cancellationToken);

        return result;
    }

    public async Task<int> InsertUserAsync(CreateUserDto input, CancellationToken cancellationToken = default)
    {
        // int s = await userRepository.InsertAsync(new User(Guid.NewGuid(), "test"), true, cancellationToken: cancellationToken);
        // List<User> users = [];
        // for (int i = 0; i < 10; i++)
        // {
        //     users.Add(new User(Guid.NewGuid(), "test") { FirstName = (i + 1).ToString() });
        // }

        return await userRepository.InsertAsync(new User(Guid.NewGuid(), Guid.Empty, "test" + Guid.NewGuid().ToString("N")) { FirstName = input.FullName }, cancellationToken: cancellationToken);
    }

    public async Task<int> UpdateUserAsync(UpdateUserDto input, CancellationToken cancellationToken = default)
    {
        var test = await userRepository.GetFirstOrDefaultAsync(x => x.FirstName == "Ahmet", cancellationToken: cancellationToken);
        test.SetEmail("mail_" + Guid.NewGuid().ToString("N").ToLower());
        _ = await userRepository.UpdateAsync(test, cancellationToken: cancellationToken);

        var testList = await userRepository.GetListAsync(new ListQueryOptions<User> { Filter = x => x.FirstName == "Ahmet", MaxResultCount = 10 }, cancellationToken: cancellationToken);
        foreach (var item in testList)
        {
            item.SetEmail("mail_" + Guid.NewGuid().ToString("N").ToLower());
        }

        _ = await userRepository.UpdateManyAsync(testList, cancellationToken: cancellationToken);

        var res2 = await userRepository.UpdateByIdAsync(test.Id, x =>
        {
            x.SetEmail("mail_" + Guid.NewGuid().ToString("N").ToLower());
            x.LastName = "dene";
        }, cancellationToken: cancellationToken);

        return 0;
    }

    public async Task<int> DeleteUserAsync(DeleteUserDto input, CancellationToken cancellationToken = default)
    {
        return await userRepository.DeleteAsync(new User(Guid.NewGuid(), Guid.NewGuid(), "test"), cancellationToken: cancellationToken);
    }

    public async Task<int> UnitOfWorkTestAsync(CancellationToken cancellationToken = default)
    {
        //  var users = await userRepository.GetListAsync(new ListQueryOptions<User>(), cancellationToken);
        //  var idlist = users.Select(x => x.Id).ToList();
        //
        var newUser2 = new User { FirstName = "insert1", LastName = "Test" };
        newUser2.SetEmail("insert1");
        await userRepository.InsertAsync(newUser2, cancellationToken: cancellationToken);

        var newUser3 = new User { FirstName = "insert2", LastName = "Test" };
        newUser3.SetEmail(newUser2.Id.ToString());
        await userRepository.InsertAsync(newUser3, cancellationToken: cancellationToken);


        //
        // await userRepository.DeleteByIdsAsync(idlist, cancellationToken: cancellationToken);
        //
        // await userRepository.UpdateByIdAsync(newUser2.Id, u => u.Email="123456789012345678901234567890123456789012345678901234567890", cancellationToken: cancellationToken);
        //
        // int ress = await userRepository.SaveChangesAsync(cancellationToken);

        var newUser = new User { FirstName = "UnitOfWork", LastName = "Test" };

        try
        {
            return await unitOfWork.ExecuteAsync(async () =>
            {
                // 1. Add
                await userRepository.InsertAsync(newUser, cancellationToken: cancellationToken);

                // 2. Update
                await userRepository.UpdateByIdAsync(Guid.Parse("0199485c-f463-7b7c-9dda-7e6fc9261201"), u => u.Age = 61, cancellationToken: cancellationToken);
            }, cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine("Database error: " + e.Message);
            return 0;
        }
    }
}
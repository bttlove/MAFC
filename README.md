Tổng quan dự án chi tiết: Base .NET API cho Quản lý Hợp đồng Bảo hiểm
Dự án này là một nền tảng (base project) ASP.NET Core Web API được thiết kế để minh họa việc tích hợp các tính năng cốt lõi và tuân thủ các thực tiễn tốt nhất (best practices) trong phát triển API hiện đại. Mục tiêu chính là cung cấp một khung sườn vững chắc, dễ mở rộng và bảo trì cho các ứng dụng API phức tạp, đặc biệt phù hợp với các yêu cầu của bạn trong lĩnh vực quản lý hợp đồng bảo hiểm. Nó tập trung vào sự rõ ràng, hiệu suất và khả năng mở rộng.

Các tính năng chính đã được triển khai chi tiết
Dự án này đã tích hợp các thành phần và tính năng sau, cùng với lý do và cách thức triển khai:

Kết nối và sử dụng SQL Server (Entity Framework Core)

Mục đích: Quản lý và lưu trữ dữ liệu hợp đồng bảo hiểm một cách bền vững và có cấu trúc.

Lý do: Entity Framework Core (EF Core) là một ORM (Object-Relational Mapper) chính thức từ Microsoft. Nó cho phép bạn tương tác với cơ sở dữ liệu SQL Server bằng cách sử dụng các đối tượng C# (mô hình "Code-First"), loại bỏ nhu cầu viết các câu lệnh SQL thô. Điều này giúp tăng tốc độ phát triển, giảm thiểu lỗi liên quan đến SQL và dễ dàng thay đổi cơ sở dữ liệu sau này (ví dụ: sang PostgreSQL, MySQL) mà không cần thay đổi nhiều code nghiệp vụ.

Mở rộng: Để thêm các bảng mới, bạn chỉ cần tạo các lớp Model mới trong thư mục Models, thêm DbSet tương ứng vào ApplicationDbContext, sau đó tạo và áp dụng Migration mới.

API Versioning

Mục đích: Cho phép phát triển, triển khai và duy trì nhiều phiên bản của API cùng lúc, đảm bảo khả năng tương thích ngược với các client cũ khi có thay đổi lớn về cấu trúc API.

Lý do: Trong một hệ thống lớn, việc thay đổi API có thể phá vỡ các ứng dụng client hiện có. Versioning giúp bạn giới thiệu các phiên bản mới mà không ảnh hưởng đến các client đang sử dụng phiên bản cũ, cho phép họ có thời gian chuyển đổi. Chúng ta sử dụng UrlSegmentApiVersionReader (phiên bản trong URL như /v1/), HeaderApiVersionReader (trong header x-api-version), và QueryStringApiVersionReader (trong query string api-version) để linh hoạt trong cách client chỉ định phiên bản.

Mở rộng: Để thêm phiên bản API mới (ví dụ: v2.0), bạn sẽ tạo một Controller mới (hoặc thêm các action mới vào Controller hiện có) và đánh dấu chúng bằng [ApiVersion("2.0")]. Swagger sẽ tự động tạo tài liệu cho phiên bản mới này.

IP Whitelisting

Mục đích: Hạn chế quyền truy cập vào API chỉ từ một danh sách các địa chỉ IP được xác định trước, tăng cường bảo mật ở lớp mạng.

Lý do: Đây là một biện pháp bảo mật cơ bản nhưng hiệu quả để bảo vệ API khỏi các truy cập trái phép từ các nguồn không đáng tin cậy hoặc bên ngoài mạng nội bộ của bạn. Việc triển khai dưới dạng Middleware tùy chỉnh (IpWhitelistMiddleware.cs) đảm bảo rằng việc kiểm tra IP diễn ra rất sớm trong pipeline xử lý yêu cầu của ASP.NET Core, trước khi yêu cầu đến được Controller, giúp tiết kiệm tài nguyên và ngăn chặn các yêu cầu không hợp lệ ngay từ đầu.

Mở rộng: Để thay đổi danh sách IP, bạn chỉ cần cập nhật mảng WhitelistedIps trong appsettings.json mà không cần biên dịch lại code.

Custom Response Wrapping (Swap Response)

Mục đích: Đảm bảo tất cả các phản hồi từ API (cả thành công và thất bại) đều tuân theo một cấu trúc JSON chuẩn hóa (status, code, message, data).

Lý do: Việc có một cấu trúc phản hồi nhất quán giúp các ứng dụng client dễ dàng xử lý kết quả hơn, giảm thiểu logic phân tích phản hồi phức tạp. Nó cũng cải thiện trải nghiệm của nhà phát triển client (DX) vì họ luôn biết cấu trúc dữ liệu sẽ trông như thế nào. ResponseWrappingMiddleware.cs hoạt động bằng cách "chụp" phản hồi gốc của Controller, bọc nó vào cấu trúc ApiResponse của chúng ta, sau đó gửi lại cho client.

Mở rộng: Nếu bạn muốn thay đổi cấu trúc phản hồi chuẩn hóa, bạn chỉ cần sửa đổi lớp ApiResponse.cs và logic trong ResponseWrappingMiddleware.cs.

Guards ngoại lệ (Custom Exception Handling)

Mục đích: Xử lý tập trung các lỗi và ngoại lệ xảy ra trong ứng dụng, trả về phản hồi lỗi chuẩn hóa và thân thiện cho client, đồng thời ghi log lỗi chi tiết.

Lý do: Việc không xử lý ngoại lệ có thể dẫn đến việc lộ thông tin nhạy cảm về lỗi nội bộ (stack traces) cho client, gây ra lỗ hổng bảo mật. ExceptionHandlingMiddleware.cs bắt tất cả các ngoại lệ không được xử lý, chuyển chúng thành các phản hồi ApiResponse với status: false và mã lỗi thích hợp (ví dụ: 400 cho lỗi validation, 500 cho lỗi server nội bộ), đồng thời ghi log lỗi để dễ dàng gỡ lỗi và giám sát.

Mở rộng: Bạn có thể thêm các loại ngoại lệ tùy chỉnh khác (ví dụ: NotFoundException, ForbiddenException) vào ExceptionHandlingMiddleware.cs để xử lý chúng một cách cụ thể và trả về các mã trạng thái HTTP phù hợp.

Task Scheduling (Hangfire)

Mục đích: Thực hiện các tác vụ nền (background tasks) định kỳ hoặc theo lịch trình mà không làm chặn luồng xử lý yêu cầu chính của API.

Lý do: Các tác vụ như dọn dẹp dữ liệu cũ, gửi email hàng loạt, tạo báo cáo định kỳ, hoặc xử lý các công việc nặng có thể mất nhiều thời gian. Việc chạy chúng trong luồng chính của API sẽ làm giảm hiệu suất và thời gian phản hồi. Hangfire cung cấp một giải pháp mạnh mẽ để quản lý các tác vụ này, với dashboard để theo dõi.

Mở rộng: Để thêm các tác vụ nền mới, bạn tạo một phương thức trong một service (ví dụ: SampleBackgroundTask.cs), sau đó sử dụng BackgroundJob.Enqueue, RecurringJob.AddOrUpdate hoặc DelayedJob của Hangfire để lên lịch cho chúng.

CORS (Cross-Origin Resource Sharing)

Mục đích: Cho phép các ứng dụng web chạy trên một tên miền khác có thể gửi yêu cầu đến API của bạn một cách an toàn.

Lý do: Các trình duyệt hiện đại áp dụng chính sách "same-origin policy" để tăng cường bảo mật, ngăn chặn các script từ một tên miền truy cập tài nguyên từ tên miền khác. CORS là một cơ chế cho phép bạn nới lỏng chính sách này một cách có kiểm soát, cần thiết khi API của bạn được sử dụng bởi các frontend ứng dụng (ví dụ: React, Angular, Vue) được host trên các tên miền khác.

Mở rộng: Bạn có thể thêm nhiều tên miền vào WithOrigins() hoặc sử dụng AllowAnyOrigin() (không khuyến nghị cho môi trường production) để cho phép bất kỳ tên miền nào truy cập.

Swagger Documentation (OpenAPI)

Mục đích: Tự động tạo tài liệu tương tác cho API của bạn, giúp các nhà phát triển dễ dàng hiểu, khám phá và kiểm thử API.

Lý do: Swagger (OpenAPI) là tiêu chuẩn công nghiệp để mô tả API RESTful. Nó cung cấp một giao diện người dùng thân thiện (Swagger UI) cho phép bạn xem tất cả các endpoint, các tham số, cấu trúc request/response, và thậm chí gửi các yêu cầu API trực tiếp từ trình duyệt mà không cần công cụ bên ngoài như Postman. Điều này cải thiện đáng kể trải nghiệm của nhà phát triển (DX).

Mở rộng: Bạn có thể thêm các XML comments (/// <summary>) vào Controller và DTOs để Swagger tự động tạo mô tả chi tiết hơn.

Tích hợp Redis Cache

Mục đích: Lưu trữ dữ liệu tạm thời trong bộ nhớ cache để tăng tốc độ truy xuất dữ liệu và giảm tải cho cơ sở dữ liệu.

Lý do: Đối với các dữ liệu ít thay đổi nhưng được truy xuất thường xuyên (ví dụ: thông tin sản phẩm, danh mục), việc đọc từ Redis (một bộ nhớ cache trong RAM) nhanh hơn nhiều so với việc truy vấn từ SQL Server. Điều này giúp cải thiện đáng kể hiệu suất và khả năng mở rộng của API. IDistributedCache là một abstraction của .NET Core cho phép bạn dễ dàng chuyển đổi giữa các nhà cung cấp cache khác nhau (Redis, SQL Server, bộ nhớ cục bộ).

Mở rộng: Bạn có thể áp dụng caching cho các endpoint khác nhau, cấu hình thời gian hết hạn cache linh hoạt và triển khai các chiến lược cache phức tạp hơn (ví dụ: cache-aside, write-through).

FluentValidation

Mục đích: Cung cấp một cách mạnh mẽ, linh hoạt và dễ kiểm thử để xác thực dữ liệu đầu vào (request DTOs).

Lý do: Thay vì sử dụng Data Annotations truyền thống (ví dụ: [Required], [StringLength]) trực tiếp trên DTO, FluentValidation cho phép bạn định nghĩa các quy tắc xác thực trong các lớp riêng biệt (*Validator.cs). Điều này giúp tách biệt mối quan tâm (separation of concerns), làm cho code sạch hơn, dễ đọc, dễ bảo trì và đặc biệt là dễ kiểm thử đơn vị (unit test) hơn. Các quy tắc validation phức tạp (ví dụ: tuổi trong khoảng, tỷ lệ bảo hiểm hợp lệ) cũng dễ dàng được thể hiện.

Mở rộng: Để thêm các quy tắc validation mới, bạn chỉ cần sửa đổi hoặc thêm các lớp Validator mới trong thư mục Validators.

AutoMapper

Mục đích: Tự động ánh xạ dữ liệu giữa các đối tượng (ví dụ: từ Request DTO sang Model Entity và ngược lại).

Lý do: Trong các ứng dụng nhiều tầng, bạn thường có các lớp DTO (Data Transfer Objects) để truyền dữ liệu qua API và các lớp Model (Entity) để tương tác với cơ sở dữ liệu. Việc chuyển đổi dữ liệu giữa các lớp này có thể tốn nhiều công sức và dễ gây lỗi nếu làm thủ công. AutoMapper tự động hóa quá trình này thông qua cấu hình, giúp code gọn gàng hơn, giảm thiểu lỗi và tăng năng suất.

Mở rộng: Để ánh xạ các đối tượng mới, bạn chỉ cần thêm các CreateMap() mới vào lớp MappingProfile.cs.

Serilog (Logging)

Mục đích: Ghi lại các sự kiện, thông tin debug, cảnh báo và lỗi của ứng dụng một cách có cấu trúc vào console và file.

Lý do: Logging là cực kỳ quan trọng cho việc gỡ lỗi (debugging), giám sát hoạt động của ứng dụng trong môi trường sản xuất và phân tích các vấn đề. Serilog là một thư viện logging mạnh mẽ, cho phép ghi log có cấu trúc (structured logging), dễ dàng tìm kiếm và phân tích bằng các công cụ quản lý log. Nó cũng hỗ trợ nhiều "sinks" (nơi ghi log) khác nhau như console, file, database, ELK stack, v.v.

Mở rộng: Bạn có thể cấu hình Serilog để ghi log vào các sinks khác (ví dụ: Elasticsearch, Azure Application Insights), thay đổi cấp độ log (MinimumLevel) cho các môi trường khác nhau, và thêm các thông tin làm giàu log (enrichers).

Cấu trúc thư mục và các tệp chính chi tiết
Dự án được tổ chức thành các thư mục để phân chia rõ ràng các trách nhiệm, tuân thủ nguyên tắc tách biệt mối quan tâm (Separation of Concerns):

Controllers/:

Mục đích: Chứa các lớp Controller xử lý các yêu cầu HTTP đến từ client. Mỗi Controller chịu trách nhiệm cho một tập hợp các tài nguyên hoặc chức năng liên quan.

Lý do: Đây là điểm vào của API. Chúng nhận request, ủy quyền logic nghiệp vụ cho các lớp Service, và trả về response. Việc sử dụng [ApiController], [Route], [ApiVersion] giúp ASP.NET Core tự động xử lý model binding, validation và định tuyến.

Tệp chính:

HubContractController.cs: Controller chính xử lý các yêu cầu liên quan đến hợp đồng bảo hiểm.

Tại sao code vậy:

[ApiController]: Kích hoạt các tính năng tiện lợi của API như tự động trả về 400 Bad Request cho lỗi model binding, suy luận nguồn tham số, và buộc sử dụng attribute routing.

[Route("api/v{version:apiVersion}/[controller]")]: Định nghĩa template URL cho Controller, bao gồm versioning và tên Controller.

[ApiVersion("1.0")]: Chỉ định phiên bản API mà Controller này hỗ trợ.

Dependency Injection (IInsuranceService, ILogger): Controller không chứa logic nghiệp vụ trực tiếp mà ủy quyền cho IInsuranceService, tuân thủ nguyên tắc Single Responsibility Principle (SRP) và giúp dễ dàng kiểm thử.

[HttpPost("MAFC_SKNVV_CreateContract")]: Định nghĩa phương thức HTTP (POST) và đường dẫn cụ thể cho action này.

ProducesResponseType: Giúp Swagger biết các loại phản hồi HTTP và kiểu dữ liệu mà action này có thể trả về, cải thiện tài liệu API.

Kiểm tra AccessKey: Một kiểm tra bảo mật đơn giản (cho mục đích demo), trong thực tế sẽ thay thế bằng Authentication/Authorization mạnh mẽ hơn.

return Ok(new { });: Trả về một phản hồi 200 OK với một đối tượng JSON rỗng, sau đó sẽ được ResponseWrappingMiddleware bọc lại.

Models/:

Mục đích: Chứa các lớp đại diện cho các thực thể dữ liệu trong cơ sở dữ liệu.

Lý do: Đây là các lớp POCO (Plain Old CLR Objects) được EF Core sử dụng để ánh xạ tới các bảng trong database. Chúng định nghĩa cấu trúc của dữ liệu được lưu trữ.

Tệp chính:

InsuranceContract.cs: Định nghĩa cấu trúc của một hợp đồng bảo hiểm, bao gồm các thuộc tính như LoanNo, CustName, LoanAmount, v.v.

Tại sao code vậy:

[Key], [DatabaseGenerated(DatabaseGeneratedOption.Identity)]: Data Annotations để EF Core biết Id là khóa chính và tự động tăng.

[Required], [MaxLength]: Các Data Annotations cơ bản để định nghĩa các ràng buộc về dữ liệu ở cấp độ Model/Database.

Sử dụng DateTime cho ngày tháng: Đảm bảo kiểu dữ liệu chính xác cho việc lưu trữ và thao tác ngày tháng. Việc chuyển đổi từ string sang DateTime được thực hiện trong MappingProfile.

Dtos/:

Mục đích: Chứa các Data Transfer Objects (DTOs), là các lớp được sử dụng để định nghĩa cấu trúc dữ liệu cho request (dữ liệu nhận từ client) và response (dữ liệu gửi về client) của API.

Lý do: Việc sử dụng DTOs giúp tách biệt cấu trúc dữ liệu của API khỏi cấu trúc của Model Entity trong database. Điều này mang lại nhiều lợi ích:

Bảo mật: Không lộ tất cả các trường của Model Entity ra ngoài API.

Linh hoạt: Cấu trúc API có thể khác với cấu trúc database.

Giảm thiểu over-fetching/under-fetching: Chỉ gửi/nhận những trường cần thiết.

Tệp chính:

InsuranceContractRequestDto.cs: Định nghĩa cấu trúc dữ liệu cho một hợp đồng bảo hiểm khi nhận từ client.

Tại sao code vậy:

[Required]: Đảm bảo trường này phải có giá trị khi gửi request.

[JsonProperty("propertyName")]: Giúp ánh xạ tên thuộc tính JSON (camelCase) với tên thuộc tính C# (PascalCase), đặc biệt hữu ích khi sử dụng Newtonsoft.Json.

Sử dụng string cho ngày tháng: Nhận ngày tháng dưới dạng chuỗi (dd/MM/yyyy) từ client để dễ dàng gửi/nhận, sau đó được AutoMapper chuyển đổi sang DateTime khi ánh xạ sang Model.

required string PropertyName { get; set; }: Sử dụng từ khóa required (C# 11+) để chỉ rõ rằng thuộc tính này phải có giá trị khi đối tượng được khởi tạo, giải quyết lỗi CS8618 khi bật Nullable Reference Types.

CreateContractRequestDto.cs: Định nghĩa cấu trúc tổng thể của request tạo hợp đồng, bao gồm access_key, product_Code và danh sách data (là InsuranceContractRequestDto).

Tại sao code vậy: Tương tự InsuranceContractRequestDto.cs về [Required] và [JsonProperty]. Nó là một "wrapper" cho danh sách các hợp đồng con.

ApiResponse.cs: Định nghĩa cấu trúc phản hồi chuẩn hóa cho tất cả các API.

Tại sao code vậy:

ApiResponse<T> (generic): Dùng cho các phản hồi thành công có kèm dữ liệu. T là kiểu dữ liệu của Data.

ApiResponse (non-generic): Dùng cho các phản hồi thành công không có dữ liệu hoặc các phản hồi lỗi.

Status, Code, Message, Data: Các trường tiêu chuẩn để client dễ dàng xử lý.

Các constructor: Đảm bảo các thuộc tính non-nullable được khởi tạo, giải quyết lỗi CS8618.

Services/:

Mục đích: Chứa logic nghiệp vụ chính của ứng dụng.

Lý do: Tách biệt logic nghiệp vụ khỏi Controller và Repository. Controller chỉ chịu trách nhiệm về HTTP, Repository chỉ chịu trách nhiệm về dữ liệu. Service là nơi chứa các quy tắc kinh doanh, phối hợp giữa các Repository và các dịch vụ khác. Điều này giúp code dễ bảo trì, dễ kiểm thử đơn vị và dễ mở rộng.

Tệp chính:

IInsuranceService.cs: Định nghĩa interface cho InsuranceService.

Tại sao code vậy: Sử dụng interface cho Dependency Injection. Điều này cho phép bạn dễ dàng thay đổi implementation của InsuranceService (ví dụ: mock nó trong kiểm thử) mà không ảnh hưởng đến Controller.

InsuranceService.cs: Triển khai logic nghiệp vụ cho việc tạo và truy vấn hợp đồng bảo hiểm.

Tại sao code vậy:

Dependency Injection (ApplicationDbContext, IMapper, IDistributedCache, ILogger, IValidator): Service nhận các dependency cần thiết qua constructor, tuân thủ nguyên tắc Inversion of Control (IoC).

Validation thủ công (_validator.ValidateAsync(dto)): Mặc dù FluentValidation.AspNetCore tự động validate trên Controller, việc validate lại trong Service đảm bảo rằng logic nghiệp vụ chỉ xử lý dữ liệu hợp lệ, ngay cả khi được gọi từ các nguồn khác không phải API.

Sử dụng _mapper.Map<InsuranceContract>(dto): Ánh xạ DTO sang Model Entity một cách tự động.

Tương tác với _context (DbContext): Thực hiện các thao tác thêm/truy vấn dữ liệu.

Sử dụng _cache (IDistributedCache): Logic kiểm tra cache trước khi truy vấn database và lưu kết quả vào cache.

Repositories/:

Mục đích: (Hiện tại trống) Sẽ chứa logic tương tác trực tiếp với cơ sở dữ liệu.

Lý do: Trong các dự án lớn hơn, bạn có thể muốn tách biệt hoàn toàn logic truy cập dữ liệu vào các lớp Repository riêng biệt (ví dụ: InsuranceContractRepository.cs). Điều này giúp Service không cần biết chi tiết về cách dữ liệu được lưu trữ và truy xuất, giúp dễ dàng thay đổi công nghệ database hoặc ORM sau này.

Data/:

Mục đích: Chứa lớp DbContext và các cấu hình liên quan đến Entity Framework Core.

Lý do: DbContext là cầu nối chính giữa ứng dụng và database khi sử dụng EF Core. Nó đại diện cho một phiên làm việc với database và cho phép bạn truy vấn, thêm, sửa, xóa các thực thể.

Tệp chính:

ApplicationDbContext.cs: Lớp kế thừa từ DbContext, chứa các DbSet cho các Model của bạn (ví dụ: InsuranceContracts).

Tại sao code vậy:

DbSet<InsuranceContract> InsuranceContracts { get; set; }: Đại diện cho một tập hợp các thực thể InsuranceContract trong database.

OnModelCreating: Phương thức để cấu hình chi tiết hơn về cách các Model ánh xạ tới database (ví dụ: định nghĩa khóa chính, khóa ngoại, kiểu dữ liệu cột).

Helpers/:

Mục đích: Chứa các lớp tiện ích chung hoặc các định nghĩa đặc biệt không thuộc về các tầng khác.

Lý do: Nơi tập trung các đoạn code có thể tái sử dụng hoặc các định nghĩa tùy chỉnh.

Tệp chính:

ValidationException.cs: Một lớp ngoại lệ tùy chỉnh.

Tại sao code vậy: Được tạo ra để đóng gói các lỗi validation từ FluentValidation thành một ngoại lệ cụ thể. Điều này cho phép ExceptionHandlingMiddleware nhận diện và xử lý riêng các lỗi validation, trả về phản hồi 400 Bad Request với cấu trúc lỗi chi tiết.

Middlewares/:

Mục đích: Chứa các middleware tùy chỉnh để can thiệp vào pipeline xử lý yêu cầu HTTP.

Lý do: Middleware là một thành phần mạnh mẽ trong ASP.NET Core, cho phép bạn thực hiện các tác vụ như logging, authentication, authorization, error handling, v.v., trước hoặc sau khi yêu cầu đến được Controller. Chúng hoạt động theo một chuỗi, và thứ tự đăng ký rất quan trọng.

Tệp chính:

ExceptionHandlingMiddleware.cs: Middleware để bắt và xử lý các ngoại lệ không được xử lý trong ứng dụng.

Tại sao code vậy: Đặt ở đầu pipeline (app.UseExceptionHandlingMiddleware();) để đảm bảo nó bắt được tất cả các ngoại lệ xảy ra sau nó. Nó định dạng lại phản hồi lỗi thành ApiResponse chuẩn hóa.

IpWhitelistMiddleware.cs: Middleware để kiểm tra địa chỉ IP của yêu cầu đến.

Tại sao code vậy: Đặt sớm trong pipeline để từ chối các yêu cầu từ IP không hợp lệ ngay lập tức, tránh lãng phí tài nguyên xử lý. Nó đọc danh sách IP từ cấu hình appsettings.json.

ResponseWrappingMiddleware.cs: Middleware để bọc tất cả các phản hồi thành công vào cấu trúc ApiResponse chuẩn hóa.

Tại sao code vậy: Đặt ở cuối pipeline (trước app.MapControllers()) để nó có thể "chụp" được phản hồi cuối cùng từ Controller hoặc các middleware khác, sau đó bọc nó lại trước khi gửi về client. Nó kiểm tra xem phản hồi đã là lỗi hay chưa để tránh bọc lại các phản hồi lỗi đã được ExceptionHandlingMiddleware xử lý.

Configurations/:

Mục đích: Chứa các lớp cấu hình cho các thư viện hoặc tính năng cụ thể.

Lý do: Tập trung các cấu hình vào một nơi giúp dễ quản lý và bảo trì.

Tệp chính:

MappingProfile.cs: Lớp cấu hình cho AutoMapper.

Tại sao code vậy: Kế thừa từ Profile của AutoMapper. Trong constructor, bạn định nghĩa các quy tắc ánh xạ giữa các cặp đối tượng (ví dụ: InsuranceContractRequestDto sang InsuranceContract), bao gồm cả việc chuyển đổi kiểu dữ liệu (ví dụ: string sang DateTime cho các trường ngày tháng).

Validators/:

Mục đích: Chứa các lớp xác thực dữ liệu sử dụng thư viện FluentValidation.

Lý do: Như đã giải thích ở phần tính năng, việc tách biệt logic validation vào các lớp riêng giúp code sạch hơn, dễ đọc, dễ bảo trì và dễ kiểm thử đơn vị.

Tệp chính:

InsuranceContractRequestDtoValidator.cs: Định nghĩa các quy tắc xác thực cho InsuranceContractRequestDto.

Tại sao code vậy: Kế thừa từ AbstractValidator<T>. Trong constructor, sử dụng các phương thức RuleFor() để định nghĩa các quy tắc (ví dụ: NotEmpty(), Length(), GreaterThanOrEqualTo(), Must()). Các phương thức Must() cho phép bạn viết logic xác thực tùy chỉnh phức tạp (ví dụ: kiểm tra tuổi, tỷ lệ bảo hiểm).

CreateContractRequestDtoValidator.cs: Định nghĩa các quy tắc xác thực cho CreateContractRequestDto.

Tại sao code vậy: Tương tự như InsuranceContractRequestDtoValidator.cs. Nó cũng sử dụng RuleForEach(x => x.Data).SetValidator(new InsuranceContractRequestDtoValidator()); để áp dụng validator cho từng phần tử trong danh sách Data, đảm bảo mọi hợp đồng con đều được validate.

BackgroundTasks/:

Mục đích: Chứa các lớp cho các tác vụ nền được quản lý bởi Hangfire.

Lý do: Tập trung các tác vụ nền vào một nơi để dễ quản lý.

Tệp chính:

SampleBackgroundTask.cs: Một ví dụ về tác vụ nền.

Tại sao code vậy: Là một lớp POCO đơn giản với một phương thức công khai (ví dụ: PerformDailyDataCleanup()) mà Hangfire có thể gọi. Nó có thể nhận các dependency qua constructor (ví dụ: ILogger) để thực hiện công việc của mình.

Program.cs:

Mục đích: Đây là tệp khởi đầu của ứng dụng ASP.NET Core, nơi mọi thứ được thiết lập.

Lý do: Trong các phiên bản .NET Core mới (từ .NET 6 trở lên), Program.cs sử dụng "top-level statements" để giảm thiểu boilerplate code. Nó chịu trách nhiệm chính trong việc:

Cấu hình Host: Thiết lập môi trường ứng dụng (development, production).

Đăng ký Services: Đăng ký tất cả các dịch vụ (như DbContext, AutoMapper, FluentValidation, Redis, Hangfire, các service nghiệp vụ) vào Dependency Injection Container. Điều này cho phép các thành phần khác yêu cầu và sử dụng các dịch vụ này mà không cần biết cách chúng được tạo ra.

Cấu hình HTTP Request Pipeline (Middleware): Định nghĩa thứ tự các middleware sẽ xử lý yêu cầu HTTP. Thứ tự này rất quan trọng vì mỗi middleware có thể thực hiện một tác vụ và sau đó chuyển yêu cầu cho middleware tiếp theo hoặc trả về phản hồi ngay lập tức.

Tại sao code vậy:

builder.Services.Add...(): Tất cả các dòng này phải nằm trước var app = builder.Build(); vì chúng đăng ký dịch vụ vào container.

app.Use...(): Các dòng này phải nằm sau var app = builder.Build(); và thứ tự của chúng rất quan trọng. Ví dụ: UseExceptionHandlingMiddleware() phải ở đầu để bắt tất cả các lỗi, UseRouting() phải trước UseCors(), UseAuthentication(), UseAuthorization(), và UseResponseWrappingMiddleware() phải ở gần cuối để bọc phản hồi cuối cùng.

dbContext.Database.Migrate(): Được gọi khi khởi động để tự động áp dụng các thay đổi database (chỉ nên dùng trong môi trường phát triển).

Log.Logger = ..., builder.Host.UseSerilog(): Cấu hình Serilog để ghi log.

appsettings.json / appsettings.Development.json:

Mục đích: Chứa các cài đặt cấu hình cho ứng dụng.

Lý do: Tách biệt cấu hình khỏi code, giúp dễ dàng thay đổi các cài đặt (ví dụ: chuỗi kết nối database, khóa API, cài đặt Redis) giữa các môi trường (phát triển, thử nghiệm, sản xuất) mà không cần biên dịch lại ứng dụng.

Tại sao WhitelistedIps vậy: WhitelistedIps là một mảng các chuỗi (string array) trong file JSON. Mỗi chuỗi là một địa chỉ IP được phép truy cập API. Middleware IpWhitelistMiddleware.cs sẽ đọc danh sách này từ cấu hình và kiểm tra địa chỉ IP của mỗi yêu cầu đến. Nếu địa chỉ IP của yêu cầu không có trong danh sách này, yêu cầu sẽ bị từ chối.

Ví dụ: "127.0.0.1" là địa chỉ localhost IPv4, "::1" là địa chỉ localhost IPv6. Bạn sẽ thêm các địa chỉ IP công cộng của máy chủ hoặc mạng nội bộ được phép truy cập API vào đây khi triển khai.

pviBase.csproj:

Mục đích: Tệp dự án C# chính.

Lý do: Định nghĩa các thuộc tính của dự án (ví dụ: TargetFramework - phiên bản .NET), các gói NuGet mà dự án phụ thuộc vào (PackageReference), và các cấu hình biên dịch khác. Khi bạn thêm một gói NuGet mới, một dòng PackageReference sẽ được thêm vào đây.

Properties/launchSettings.json:

Mục đích: Định nghĩa các profile khởi chạy ứng dụng trong môi trường phát triển.

Lý do: Cho phép bạn cấu hình các cách khác nhau để chạy ứng dụng (ví dụ: với IIS Express, hoặc trực tiếp bằng dotnet run qua HTTP/HTTPS), bao gồm cổng, URL khởi chạy mặc định, và biến môi trường.

Tại sao code vậy:

"launchUrl": "swagger": Đã được sửa để trình duyệt tự động mở đến Swagger UI khi bạn chạy ứng dụng, thay vì endpoint weatherforecast mặc định.

"applicationUrl": Định nghĩa các URL mà ứng dụng sẽ lắng nghe (cổng HTTP và HTTPS).

"environmentVariables": Đặt biến môi trường (ví dụ: ASPNETCORE_ENVIRONMENT) để ứng dụng biết nó đang chạy trong môi trường nào (Development, Production)
Chạy ứng dụng: Nhấn F5 trong Visual Studio hoặc chạy dotnet run từ terminal. Trình duyệt sẽ tự động mở đến Swagger UI.

Kiểm thử API
Sau khi ứng dụng chạy, trình duyệt sẽ mở đến Swagger UI (thường là https://localhost:<port>/swagger).

Kiểm thử POST /api/v1/HubContract/MAFC_SKNVV_CreateContract:

Mở rộng endpoint này.

Nhấp "Try it out".

Dán Request Body mẫu (đảm bảo access_key khớp với giá trị trong HubContractController.cs, ví dụ: "2672ECD7-97F3-4ABE-9B6E-3415BCBDA1C2").

Nhấn "Execute".

Kiểm tra phản hồi:

200 OK: Thành công.

400 Bad Request: Lỗi validation (kiểm tra Response body để biết chi tiết lỗi).

401 Unauthorized: access_key không khớp.

Kiểm thử GET /api/v1/HubContract/{loanNo}:

Mở rộng endpoint này.

Nhấp "Try it out".

Nhập một loanNo đã được tạo thành công.

Nhấn "Execute".

Kiểm tra phản hồi.

Kiểm thử Hangfire Dashboard:

Truy cập https://localhost:<port>/hangfire để xem các tác vụ nền.

Các vấn đề thường gặp và cách xử lý
Lỗi CS8618 (Non-nullable property...): Đảm bảo tất cả các thuộc tính non-nullable trong DTOs và Models của bạn được khởi tạo trong constructor hoặc được đánh dấu là required.

Lỗi NU1102 (Unable to find package...):

Đảm bảo bạn đã cài đặt tất cả các package cần thiết và file .csproj đã được cập nhật đúng.

Xóa cache NuGet: dotnet nuget locals all --clear (chạy với quyền Administrator, đảm bảo không có tiến trình .NET nào đang chạy).

Khôi phục lại package: dotnet restore.

Kiểm tra lại các nguồn NuGet trong Visual Studio (Tools -> NuGet Package Manager -> Package Manager Settings -> Package Sources).

Lỗi "Your connection is not private" (HTTPS): Chạy dotnet dev-certs https --trust với quyền Administrator và khởi động lại trình duyệt.

API trả về 400 Bad Request: Kiểm tra Response body trong Developer Tools của trình duyệt (tab Network) để xem chi tiết lỗi validation. Đảm bảo dữ liệu bạn gửi khớp với yêu cầu của API (định dạng, giá trị, trường bắt buộc).

API trả về 401 Unauthorized: Kiểm tra giá trị access_key bạn gửi trong request body có khớp chính xác với giá trị được kiểm tra trong HubContractController.cs hay không.

Trình duyệt mở /weatherforecast thay vì /swagger: Chỉnh sửa file Properties/launchSettings.json, thay đổi launchUrl thành "swagger" trong các profile khởi chạy.

Dự án này cung cấp một nền tảng vững chắc để bạn có thể mở rộng thêm các tính năng nghiệp vụ và bảo mật khác.


Các bước để tạo một dự án mới cho đối tác khác dựa trên base này
Cách 1: Sao chép dự án hiện có (Phổ biến và đơn giản nhất)

Đây là cách nhanh nhất để bắt đầu một dự án mới độc lập cho mỗi đối tác.

Sao chép thư mục dự án:

Đóng Visual Studio.

Điều hướng đến thư mục chứa project pviBase của bạn trên ổ đĩa.

Sao chép toàn bộ thư mục pviBase và dán nó vào một vị trí mới.

Đổi tên thư mục đã sao chép thành tên project mới (ví dụ: PartnerX.Api).

Đổi tên project và Solution trong Visual Studio:

Mở thư mục PartnerX.Api mới trong Visual Studio.

Trong Solution Explorer, nhấp chuột phải vào tên project cũ (pviBase).

Chọn "Rename" và đổi tên thành tên project mới (ví dụ: PartnerX.Api).

Nhấp chuột phải vào Solution (thường là Solution 'pviBase' (1 of 1 project)).

Chọn "Rename" và đổi tên thành tên Solution mới (ví dụ: PartnerX.Api.sln).

Quan trọng: Sau khi đổi tên, hãy lưu tất cả (File -> Save All) và sau đó đóng Visual Studio và mở lại Solution mới. Điều này giúp Visual Studio cập nhật các tham chiếu internal.

Cập nhật các namespace:

Sau khi đổi tên project, các namespace trong code vẫn là pviBase. Bạn cần đổi chúng thành PartnerX.Api (hoặc tên project mới của bạn).

Cách nhanh nhất: Trong Visual Studio, vào menu Edit > Find and Replace > Replace in Files (Ctrl+Shift+H).

Tìm kiếm: namespace pviBase

Thay thế bằng: namespace PartnerX.Api

Chọn "Entire Solution" hoặc "Current Project" và nhấn "Replace All".

Lặp lại cho bất kỳ using pviBase.xyz; nào cần thay đổi (ví dụ: using PartnerX.Api.Dtos;).

Tùy chỉnh cho đối tác mới:

Database: Cập nhật ConnectionStrings:DefaultConnection trong appsettings.json để trỏ đến database riêng của đối tác mới.

Bạn sẽ cần tạo database mới và chạy lại Add-Migration InitialCreate và Update-Database (hoặc dotnet ef database update) để tạo schema trong database mới.

DTOs & Models:

Nếu cấu trúc dữ liệu cho đối tác mới khác, bạn sẽ sửa đổi các file trong Models và Dtos cho phù hợp.

Ví dụ: Nếu đối tác X có các trường dữ liệu khác cho hợp đồng, bạn sẽ sửa InsuranceContract.cs, InsuranceContractRequestDto.cs, v.v.

Validators:


Cập nhật các quy tắc validation trong thư mục Validators để phù hợp với yêu cầu của đối tác mới.

Business Logic (Services):

Thay đổi logic trong InsuranceService.cs (hoặc tạo các Service mới) để xử lý các quy tắc nghiệp vụ đặc thù của đối tác.

Controllers:

Sửa đổi HubContractController.cs (hoặc tạo Controller mới) nếu endpoint hoặc logic xử lý request khác.

Đặc biệt: Thay đổi access_key trong HubContractController.cs thành khóa bí mật riêng của đối tác mới.

IP Whitelist: Cập nhật IpWhitelist:WhitelistedIps trong appsettings.json với các địa chỉ IP của đối tác mới.

CORS: Cập nhật Cors:AllowSpecificOrigin trong appsettings.json với các domain frontend của đối tác mới.

Redis Cache: Nếu đối tác có Redis riêng, cập nhật RedisCacheSettings:ConnectionString.

Hangfire: Nếu đối tác có yêu cầu về các tác vụ nền khác, bạn sẽ sửa đổi hoặc thêm các tác vụ trong BackgroundTasks.

Swagger: Tài liệu Swagger sẽ tự động cập nhật theo các thay đổi của Contro
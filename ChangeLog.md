## Trong file layout:
- @RenderBody() : Đặt tại vị trí mà nội dung của các trang web sẽ được "ghi" vào đó
- @{
     await Html.RenderPartialAsync("PartialView");
   }
   hoặc:
   @await Html.PartialAsync("PartialView")
   Dùng để lấy nội dung của một PartialView (phần code HTML được tách ra ở 1 file view) và "ghi/chèn" vào một vị trí nào đó.
- @await RenderSectionAsync("SectionName", required: false)
  


## Phác thảo chức năng cho SV22T1020146.Admin

- Trang chủ: Home/Index
- Tài khoản:
    - Account/Login
    - Account/Logout
    - Account/ChangPassword
- Supplier :
    - Supplier/Index
    - Supplier/Create
    - Supplier/Edit/{id}
	- Supperlier/Delete/{id}
- Customer :
    - Customer/Index
	- Customer/Create
	- Customer/Edit/{id}
	- Customer/Delete/{id}
	- Customer/ChangePassword/{id}
- Shipper :
    - Shipper/Index
    - Shipper/Create
    - Shipper/Edit/{id}
	- Shipper/Delete/{id}
_ Employee :
	- Employee/Index
	- Empoyee/Create
    - Employee/edit/{id}
	- Employee/Delete/{id}
	- Employee/ChangePassword/{id}
	- Employee/ChangeRoles/{id}
_ Cagegory :
	- Category/Index
	- Category/Create
	- Category/Edit/{id}
	- Category/Delete/{id}
- Product :
	- Product/Index
	    - Tìm kiếm, lọc mặt hàng theo nhà cung cấp, phân loại, khoảng giá, tên
	    - Hiển thị danh sách dưới dạng phân trang
	- Product/Create
	- Product/Edit/{id}
	- Product/Delete/{id}
	- Product/Detail/{id}
	- Product/ListAtrributes/{id}
	- Product/AddAtrribute/{id}
	- Product/EditAttribute/{id}?attributeId={attributeId}
	- Product/DeleteAttribute/{id}?attributeId={attributeId}
	- Product/ListPhotos/{id}
	- Product/AddPhoto/{id}
	- Product/DeletePhoto/{id}?photoId={photoId}
	- Product/DeletePhoto/{id}?photoId={photoId}
- Order :
	- Order/Index
    - Order/Search
    - Order/Create
    - Order/Detail/{id}
	- Order/EditCartItem/{id}?productId={productId}
	- Order/DeleteCartItem/{id}?productId={productId}
	- Order/ClearCart
	- Order/Accept/{id}
	- Order/Shipping/{id}
	- Order/Finish/{id}
	- Order/Reject/{id}
	- Order/Cancel/{id}
	- Order/Delete/{id}
  

## Models chia theo Domain:
	- Data dictionary: Province
	- Partner: Supplier, Customer, Shipper
	- HR(Human Resource): Employee
	- Catalog: Category, Product, ProductAttribute, ProductPhoto
	- Sales: Order, OrderStatus, OrderDetail
	- Security: UserAccount
	- Common: ...

- Tìm kiếm phân trang: Đầu vào tìm kiếm, phân trang: Page, PageSize, SearchValue(Nhà cc, khách hàng, shipper, cateory, employee)
- Lấy thông tin của 1 đối tuong dụa vào id
- Bổ sung1 đối tuong vào CSDL
- Cập nhật 1 đối tuong trong CSDL
- Xóa 1 đối tuong ra khỏi CSDL dụa vào id
- Kiểm tra xem 1 đối tuong có dũ liệu liên quan hay không?


ctrl + r + r: đổi tên tất cả
ViewInfo: Chi tiết
SearchInfo: Kết quả tìm kiếm

# 12/03/2026
- Cài package NewtonSoft.Json
- Trong Admin, tạo thư mục AppCodes, copy file ApplicationContext.cs vào thư mục này
- Sửa lại code của Program.cs theo mẫu
- Tạo 1 lop trong AppCodes thì xóa đuôi .AppCode trong namespace

# 16/03/2026
	- Khi Action có trả du liệu về cho view thì Phải biết kiểu du liệu là gì
	- Trong view (trên cùng), phải có chỉ thị khai báo kiểu du liệu mà Action trả về 
	@model Kiểu_du_lieu
	- Trong view, du liệu mà Action trả về luu trong thuộc tính có tên là Model 
	(trong View thông qua thuộc tính này để lấy du liệu)

TODO:
	- Phần tìm kiếm, Loại hàng và nhà cung cấp vẫn chua có option để lua chọn
	- Quản lí đon hàng

# 19/03/2026
	- Su dung modelState để kiểm soát du liệu đầu vào
	- 2 hàm trả về kiểu du liệu khác nhau thì không thể dùng chung 1 view 
	- phải có thuoc tính name, nếu không có/không khóp sẽ không gui đuoc du liệu về cho action
	- asp-for khi chạy sẽ sinh ra name="Thuoc_tinh" và value="Gia_tri" trong thẻ input
	- Luu du liệu phải có try catch để bắt lỗi, nếu không sẽ bị lỗi 500

	Đã Chỉnh CustomerController
	ChangePass giống update

# 23/03/2026
	- Nguoi dùng cung cấp thông tin dể kiểm tra xem có đioc phép vào hệ thống hay không?
	- Hệ thống kiểm tra(Authentication), nếu hop lệ thì cấp cho một Cookie(giấy chúng nhận)
	- Phia client xuất trnhf Cookie mỗi khi thuc hiện các Request (kèm cookie trong header của loi gọi)
	- Phía server du vào cookie để kiểm tra(Authorization)
	** 2 Thuật ngu 
	- Authentication
	- Authorization


	Today interacted with: 

	AccountController
	CustomerController([Authorize])
	OrderController([Authorize(Roles = $"{WebUserRoles.Sales}")])

	DONE:
	SecurityDataService
	Employee: ChangeRoles, ChangePassword

	TODO: Edit, Delete, Create: Category, Product, Order

# 26/03/2026
	- Interact ShoppingCartHelper, SearchProduct, ShowCart, Create, DeleteCartItem, EditCartItem, ClearCart, SaleDataService AddOrderAsync(), ApiResult
	- OrderController

	- OrderController CreateOrder(), SalesDataService AddOrderAsync()

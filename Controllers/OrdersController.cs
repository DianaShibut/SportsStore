using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsStore.Models;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;


namespace SportsStore.Controllers
{

    public class OrdersController : Controller {
        private DataContext context;
        private IRepository productRepository;
        private IOrdersRepository ordersRepository;
        public OrdersController(IRepository productRepo,
                IOrdersRepository orderRepo, DataContext ctx)
            {context = ctx;
            productRepository = productRepo;
            ordersRepository = orderRepo; }
        
        public IActionResult Index() {
            var orders = context.Orders.FromSql("GetOrders");
            foreach(var order in orders)
            {
                order.Lines = context.OrderLines.FromSql($"GetOrdersLines @id = {order.Id}").ToList();
                foreach (var line in order.Lines)
                {
                    line.Product = context.Products.FromSql($"GetProduct @id = {line.ProductId}").ToList().First();
                }
            }
            return View(orders);
        }


        public IActionResult EditOrder(long id){
            Order order;
            if (id == 0)
            {
                order= new Order();
            }
            else
            {
                order = context.Orders.FromSql($"GetOrder @id = {id}").First();
            }
            order.Lines = context.OrderLines.FromSql($"GetOrdersLines @id = {id}").ToList();
            List<Product> orderProductList = new List<Product>();
            foreach (var line in order.Lines)
            {
                line.Product = context.Products.FromSql($"GetProduct @id = {line.ProductId}").ToList().First();
                line.Product.Category = context.Categories.FromSql($"GetCategory @id = {line.Product.CategoryId}").ToList().First();
                orderProductList.Add(line.Product);
            }
            ViewBag.Lines = order.Lines;
            var products = context.Products.FromSql($"GetProducts").ToList();
            foreach (var product in products)
            {
                if (!orderProductList.Contains(product))
                {
                    OrderLine line = new OrderLine { Product = product, ProductId = product.Id, Quantity = 0 };
                    line.Product = context.Products.FromSql($"GetProduct @id = {line.ProductId}").ToList().First();
                    line.Product.Category = context.Categories.FromSql($"GetCategory @id = {line.Product.CategoryId}").ToList().First();
                    ViewBag.Lines.Add(line);
                }
            }
            return View(order);
        }

        [HttpPost]
        public IActionResult AddOrUpdateOrder(Order order) {
            var currentOrder = context.Orders.FromSql($"EXEC AddOrUpdateOrder " +
                $"@id = {order.Id}, @address = {order.Address}, " +
                $"@customerName = {order.CustomerName}, @shipped = {order.Shipped}," +
                $" @state = {order.State}, @zipCode = {order.ZipCode}").ToList();
            foreach (var orderLine in order.Lines)
            {
                if (orderLine.Quantity > 0)
                {
                    context.Database.ExecuteSqlCommand($"EXEC AddOrUpdateOrderLines " +
                    $"@orderId = {order.Id}, @currentOrderId = {currentOrder[0].Id}," +
                    $" @productId = {orderLine.ProductId}, @orderLineId = {orderLine.Id}," +
                    $" @quantity = {orderLine.Quantity}");
                }
            }
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        public IActionResult DeleteOrder(Order order) {
            context.Database.ExecuteSqlCommand($"EXEC DeleteOrder @id = {order.Id}");
            return RedirectToAction(nameof(Index));
        }
    }
}

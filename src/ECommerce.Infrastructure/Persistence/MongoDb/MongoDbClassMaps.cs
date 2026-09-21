using System.Reflection;
using ECommerce.Domain.Entities;
using ECommerce.Domain.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace ECommerce.Infrastructure.Persistence.MongoDb;

public static class MongoDbClassMaps
{
    private static int _registered;

    public static void RegisterAll()
    {
        if (Interlocked.CompareExchange(ref _registered, 1, 0) == 1)
        {
            return;
        }

        var pack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new IgnoreExtraElementsConvention(true),
            new IgnoreIfNullConvention(true)
        };
        ConventionRegistry.Register("ECommerceConventions", pack, t => t.Namespace?.StartsWith("ECommerce.Domain", StringComparison.Ordinal) == true);

        RegisterMoney();
        RegisterCategory();
        RegisterProduct();
        RegisterCartItem();
        RegisterCart();
        RegisterOrderItem();
        RegisterOrder();
    }

    private static ConstructorInfo PrivateCtor<T>() =>
        typeof(T).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes)!;

    private static void RegisterMoney()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Money)))
        {
            return;
        }

        var ctor = typeof(Money).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            new[] { typeof(decimal), typeof(string) })!;

        BsonClassMap.RegisterClassMap<Money>(cm =>
        {
            cm.MapConstructor(ctor, "Amount", "Currency");
            cm.MapProperty(m => m.Amount).SetElementName("amount");
            cm.MapProperty(m => m.Currency).SetElementName("currency");
        });
    }

    private static void RegisterCategory()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Category)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<Category>(cm =>
        {
            cm.AutoMap();
            cm.MapConstructor(PrivateCtor<Category>());
            cm.UnmapProperty(c => c.Products);
            cm.GetMemberMap(c => c.Id).SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            cm.MapProperty(c => c.Name).SetElementName("name");
            cm.MapProperty(c => c.Slug).SetElementName("slug");
            cm.MapProperty(c => c.Description).SetElementName("description");
            cm.MapProperty(c => c.CreatedAt).SetElementName("createdAt");
            cm.MapProperty(c => c.UpdatedAt).SetElementName("updatedAt");
        });
    }

    private static void RegisterProduct()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Product)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<Product>(cm =>
        {
            cm.AutoMap();
            cm.MapConstructor(PrivateCtor<Product>());
            cm.GetMemberMap(p => p.Id).SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            cm.MapProperty(p => p.Name).SetElementName("name");
            cm.MapProperty(p => p.Slug).SetElementName("slug");
            cm.MapProperty(p => p.Description).SetElementName("description");
            cm.MapProperty(p => p.Price).SetElementName("price");
            cm.MapProperty(p => p.CategoryId).SetElementName("categoryId")
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            cm.MapProperty(p => p.Category).SetElementName("category");
            cm.MapProperty(p => p.StockQuantity).SetElementName("stockQuantity");
            cm.MapProperty(p => p.ImageUrl).SetElementName("imageUrl");
            cm.MapProperty(p => p.CreatedAt).SetElementName("createdAt");
            cm.MapProperty(p => p.UpdatedAt).SetElementName("updatedAt");
        });
    }

    private static void RegisterCartItem()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(CartItem)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<CartItem>(cm =>
        {
            cm.AutoMap();
            cm.MapConstructor(PrivateCtor<CartItem>());
            cm.UnmapProperty(ci => ci.Cart);
            cm.GetMemberMap(ci => ci.Id).SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            cm.MapProperty(ci => ci.CartId).SetElementName("cartId")
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            cm.MapProperty(ci => ci.ProductId).SetElementName("productId")
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            cm.MapProperty(ci => ci.Product).SetElementName("product");
            cm.MapProperty(ci => ci.Quantity).SetElementName("quantity");
            cm.MapProperty(ci => ci.UnitPrice).SetElementName("unitPrice");
        });
    }

    private static void RegisterCart()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Cart)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<Cart>(cm =>
        {
            cm.AutoMap();
            cm.MapConstructor(PrivateCtor<Cart>());
            cm.MapField("_items").SetElementName("items");
            cm.UnmapProperty(c => c.Items);
            cm.GetMemberMap(c => c.Id).SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            cm.MapProperty(c => c.CustomerId).SetElementName("customerId")
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            cm.MapProperty(c => c.CreatedAt).SetElementName("createdAt");
            cm.MapProperty(c => c.UpdatedAt).SetElementName("updatedAt");
        });
    }

    private static void RegisterOrderItem()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(OrderItem)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<OrderItem>(cm =>
        {
            cm.AutoMap();
            cm.MapConstructor(PrivateCtor<OrderItem>());
            cm.UnmapProperty(oi => oi.Order);
            cm.GetMemberMap(oi => oi.Id).SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            cm.MapProperty(oi => oi.OrderId).SetElementName("orderId")
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            cm.MapProperty(oi => oi.ProductId).SetElementName("productId")
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            cm.MapProperty(oi => oi.ProductNameSnapshot).SetElementName("productNameSnapshot");
            cm.MapProperty(oi => oi.Quantity).SetElementName("quantity");
            cm.MapProperty(oi => oi.UnitPriceSnapshot).SetElementName("unitPriceSnapshot");
        });
    }

    private static void RegisterOrder()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Order)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<Order>(cm =>
        {
            cm.AutoMap();
            cm.MapConstructor(PrivateCtor<Order>());
            cm.MapField("_items").SetElementName("items");
            cm.UnmapProperty(o => o.Items);
            cm.GetMemberMap(o => o.Id).SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            cm.MapProperty(o => o.CustomerId).SetElementName("customerId")
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            cm.MapProperty(o => o.TotalAmount).SetElementName("totalAmount");
            cm.MapProperty(o => o.Status).SetElementName("status")
                .SetSerializer(new EnumSerializer<Domain.Enums.OrderStatus>(BsonType.String));
            cm.MapProperty(o => o.CreatedAt).SetElementName("createdAt");
            cm.MapProperty(o => o.PaidAt).SetElementName("paidAt");
            cm.MapProperty(o => o.ShippedAt).SetElementName("shippedAt");
            cm.MapProperty(o => o.DeliveredAt).SetElementName("deliveredAt");
        });
    }
}

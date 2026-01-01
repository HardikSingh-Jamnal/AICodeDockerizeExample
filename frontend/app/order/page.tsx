"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import {
  productsApi,
  ordersApi,
  Product,
  OrderItem,
  CreateOrderRequest,
  Order,
} from "@/lib/api";

export default function OrderPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [orders, setOrders] = useState<Order[]>([]);
  const [orderItems, setOrderItems] = useState<OrderItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const [customerData, setCustomerData] = useState({
    customerId: 1,
    customerName: "",
    customerEmail: "",
    shippingAddress: "",
  });

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      const [productsData, ordersData] = await Promise.all([
        productsApi.getAll(),
        ordersApi.getAll().catch(() => []), // Don't fail if orders API is down
      ]);
      setProducts(productsData);
      setOrders(ordersData);
    } catch (err) {
      setError("Failed to load data");
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const addToOrder = (product: Product) => {
    const existingItem = orderItems.find(
      (item) => item.productId === product.id
    );

    if (existingItem) {
      setOrderItems(
        orderItems.map((item) =>
          item.productId === product.id
            ? { ...item, quantity: item.quantity + 1 }
            : item
        )
      );
    } else {
      setOrderItems([
        ...orderItems,
        {
          productId: product.id,
          productName: product.name,
          unitPrice: product.price,
          quantity: 1,
        },
      ]);
    }
  };

  const updateQuantity = (productId: number, quantity: number) => {
    if (quantity <= 0) {
      setOrderItems(orderItems.filter((item) => item.productId !== productId));
    } else {
      setOrderItems(
        orderItems.map((item) =>
          item.productId === productId ? { ...item, quantity } : item
        )
      );
    }
  };

  const getTotalAmount = () => {
    return orderItems.reduce(
      (total, item) => total + item.unitPrice * item.quantity,
      0
    );
  };

  const handleSubmitOrder = async (e: React.FormEvent) => {
    e.preventDefault();

    if (orderItems.length === 0) {
      setError("Please add items to your order");
      return;
    }

    try {
      const orderRequest: CreateOrderRequest = {
        customerId: customerData.customerId,
        customerName: customerData.customerName,
        customerEmail: customerData.customerEmail,
        shippingAddress: customerData.shippingAddress,
        orderItems,
      };

      await ordersApi.create(orderRequest);
      setSuccess("Order created successfully!");
      setOrderItems([]);
      setCustomerData({
        customerId: 1,
        customerName: "",
        customerEmail: "",
        shippingAddress: "",
      });
      await loadData();
    } catch (err) {
      setError("Failed to create order");
      console.error(err);
    }
  };

  if (loading) return <div>Loading...</div>;

  return (
    <div>
      <nav>
        <Link href="/">← Back to Home</Link>
      </nav>

      <h1>Place an Order</h1>

      {error && (
        <div
          style={{
            color: "red",
            marginBottom: "20px",
            padding: "10px",
            border: "1px solid red",
            borderRadius: "5px",
          }}
        >
          {error}
        </div>
      )}
      {success && (
        <div
          style={{
            color: "green",
            marginBottom: "20px",
            padding: "10px",
            border: "1px solid green",
            borderRadius: "5px",
          }}
        >
          {success}
        </div>
      )}

      <div
        style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "30px" }}
      >
        {/* Product Selection */}
        <div>
          <h2>Available Products</h2>
          {products.length === 0 ? (
            <p>
              No products available.{" "}
              <Link href="/products">Add some products first</Link>.
            </p>
          ) : (
            <div style={{ display: "grid", gap: "15px" }}>
              {products
                .filter((p) => p.isActive && p.stockQuantity > 0)
                .map((product) => (
                  <div
                    key={product.id}
                    style={{
                      border: "1px solid #ddd",
                      borderRadius: "5px",
                      padding: "15px",
                      display: "flex",
                      justifyContent: "space-between",
                      alignItems: "center",
                    }}
                  >
                    <div>
                      <h4>{product.name}</h4>
                      <p>{product.description}</p>
                      <p>
                        <strong>${product.price.toFixed(2)}</strong> | Stock:{" "}
                        {product.stockQuantity}
                      </p>
                    </div>
                    <button
                      onClick={() => addToOrder(product)}
                      style={{
                        padding: "8px 16px",
                        backgroundColor: "#28a745",
                        color: "white",
                        border: "none",
                        borderRadius: "5px",
                        cursor: "pointer",
                      }}
                    >
                      Add to Order
                    </button>
                  </div>
                ))}
            </div>
          )}
        </div>

        {/* Order Form */}
        <div>
          <h2>Your Order</h2>

          {/* Order Items */}
          <div style={{ marginBottom: "20px" }}>
            <h3>Items</h3>
            {orderItems.length === 0 ? (
              <p>No items in order</p>
            ) : (
              <div>
                {orderItems.map((item) => (
                  <div
                    key={item.productId}
                    style={{
                      display: "flex",
                      justifyContent: "space-between",
                      alignItems: "center",
                      padding: "10px",
                      border: "1px solid #ddd",
                      borderRadius: "3px",
                      marginBottom: "10px",
                    }}
                  >
                    <div>
                      <strong>{item.productName}</strong>
                      <br />${item.unitPrice.toFixed(2)} each
                    </div>
                    <div
                      style={{
                        display: "flex",
                        alignItems: "center",
                        gap: "10px",
                      }}
                    >
                      <button
                        onClick={() =>
                          updateQuantity(item.productId, item.quantity - 1)
                        }
                      >
                        -
                      </button>
                      <span>{item.quantity}</span>
                      <button
                        onClick={() =>
                          updateQuantity(item.productId, item.quantity + 1)
                        }
                      >
                        +
                      </button>
                      <span>
                        ${(item.unitPrice * item.quantity).toFixed(2)}
                      </span>
                    </div>
                  </div>
                ))}
                <div
                  style={{
                    textAlign: "right",
                    fontSize: "18px",
                    fontWeight: "bold",
                    marginTop: "10px",
                  }}
                >
                  Total: ${getTotalAmount().toFixed(2)}
                </div>
              </div>
            )}
          </div>

          {/* Customer Information */}
          <form onSubmit={handleSubmitOrder}>
            <h3>Customer Information</h3>

            <div style={{ marginBottom: "15px" }}>
              <label>Customer Name:</label>
              <input
                type="text"
                value={customerData.customerName}
                onChange={(e) =>
                  setCustomerData({
                    ...customerData,
                    customerName: e.target.value,
                  })
                }
                required
                style={{
                  width: "100%",
                  padding: "8px",
                  marginTop: "5px",
                  border: "1px solid #ddd",
                  borderRadius: "3px",
                }}
              />
            </div>

            <div style={{ marginBottom: "15px" }}>
              <label>Email:</label>
              <input
                type="email"
                value={customerData.customerEmail}
                onChange={(e) =>
                  setCustomerData({
                    ...customerData,
                    customerEmail: e.target.value,
                  })
                }
                required
                style={{
                  width: "100%",
                  padding: "8px",
                  marginTop: "5px",
                  border: "1px solid #ddd",
                  borderRadius: "3px",
                }}
              />
            </div>

            <div style={{ marginBottom: "20px" }}>
              <label>Shipping Address:</label>
              <textarea
                value={customerData.shippingAddress}
                onChange={(e) =>
                  setCustomerData({
                    ...customerData,
                    shippingAddress: e.target.value,
                  })
                }
                required
                style={{
                  width: "100%",
                  padding: "8px",
                  marginTop: "5px",
                  border: "1px solid #ddd",
                  borderRadius: "3px",
                  minHeight: "80px",
                }}
              />
            </div>

            <button
              type="submit"
              disabled={orderItems.length === 0}
              style={{
                width: "100%",
                padding: "15px",
                backgroundColor: orderItems.length === 0 ? "#ccc" : "#0070f3",
                color: "white",
                border: "none",
                borderRadius: "5px",
                cursor: orderItems.length === 0 ? "not-allowed" : "pointer",
                fontSize: "16px",
              }}
            >
              Place Order (${getTotalAmount().toFixed(2)})
            </button>
          </form>
        </div>
      </div>

      {/* Recent Orders */}
      {orders.length > 0 && (
        <div style={{ marginTop: "40px" }}>
          <h2>Recent Orders</h2>
          <div style={{ display: "grid", gap: "15px" }}>
            {orders.slice(0, 5).map((order) => (
              <div
                key={order.id}
                style={{
                  border: "1px solid #ddd",
                  borderRadius: "5px",
                  padding: "15px",
                }}
              >
                <h4>Order #{order.id}</h4>
                <p>
                  <strong>Customer:</strong> {order.customerName} (
                  {order.customerEmail})
                </p>
                <p>
                  <strong>Total:</strong> ${order.totalAmount.toFixed(2)}
                </p>
                <p>
                  <strong>Status:</strong>{" "}
                  {
                    [
                      "Pending",
                      "Confirmed",
                      "Shipped",
                      "Delivered",
                      "Cancelled",
                    ][order.status]
                  }
                </p>
                <p>
                  <strong>Date:</strong>{" "}
                  {new Date(order.orderDate).toLocaleDateString()}
                </p>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

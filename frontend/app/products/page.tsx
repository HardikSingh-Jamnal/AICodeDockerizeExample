"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import { productsApi, Product, CreateProductRequest } from "@/lib/api";

export default function ProductsPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editingProduct, setEditingProduct] = useState<Product | null>(null);
  const [formData, setFormData] = useState<CreateProductRequest>({
    name: "",
    description: "",
    price: 0,
    stockQuantity: 0,
    category: "",
  });

  useEffect(() => {
    loadProducts();
  }, []);

  const loadProducts = async () => {
    try {
      setLoading(true);
      const data = await productsApi.getAll();
      setProducts(data);
    } catch (err) {
      setError("Failed to load products");
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editingProduct) {
        await productsApi.update(editingProduct.id, formData);
      } else {
        await productsApi.create(formData);
      }
      await loadProducts();
      resetForm();
    } catch (err) {
      setError("Failed to save product");
      console.error(err);
    }
  };

  const handleDelete = async (id: number) => {
    if (confirm("Are you sure you want to delete this product?")) {
      try {
        await productsApi.delete(id);
        await loadProducts();
      } catch (err) {
        setError("Failed to delete product");
        console.error(err);
      }
    }
  };

  const startEdit = (product: Product) => {
    setEditingProduct(product);
    setFormData({
      name: product.name,
      description: product.description,
      price: product.price,
      stockQuantity: product.stockQuantity,
      category: product.category,
    });
    setShowForm(true);
  };

  const resetForm = () => {
    setFormData({
      name: "",
      description: "",
      price: 0,
      stockQuantity: 0,
      category: "",
    });
    setEditingProduct(null);
    setShowForm(false);
  };

  if (loading) return <div>Loading products...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <div>
      <nav>
        <Link href="/">← Back to Home</Link>
      </nav>

      <h1>Products Management</h1>

      <button
        onClick={() => setShowForm(!showForm)}
        style={{
          marginBottom: "20px",
          padding: "10px 20px",
          backgroundColor: "#0070f3",
          color: "white",
          border: "none",
          borderRadius: "5px",
          cursor: "pointer",
        }}
      >
        {showForm ? "Cancel" : "Add New Product"}
      </button>

      {showForm && (
        <form
          onSubmit={handleSubmit}
          style={{
            marginBottom: "30px",
            padding: "20px",
            border: "1px solid #ddd",
            borderRadius: "5px",
          }}
        >
          <h3>{editingProduct ? "Edit Product" : "Add New Product"}</h3>

          <div style={{ marginBottom: "15px" }}>
            <label>Name:</label>
            <input
              type="text"
              value={formData.name}
              onChange={(e) =>
                setFormData({ ...formData, name: e.target.value })
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
            <label>Description:</label>
            <textarea
              value={formData.description}
              onChange={(e) =>
                setFormData({ ...formData, description: e.target.value })
              }
              style={{
                width: "100%",
                padding: "8px",
                marginTop: "5px",
                border: "1px solid #ddd",
                borderRadius: "3px",
                minHeight: "60px",
              }}
            />
          </div>

          <div style={{ marginBottom: "15px" }}>
            <label>Price:</label>
            <input
              type="number"
              step="0.01"
              value={formData.price}
              onChange={(e) =>
                setFormData({
                  ...formData,
                  price: parseFloat(e.target.value) || 0,
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
            <label>Stock Quantity:</label>
            <input
              type="number"
              value={formData.stockQuantity}
              onChange={(e) =>
                setFormData({
                  ...formData,
                  stockQuantity: parseInt(e.target.value) || 0,
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
            <label>Category:</label>
            <input
              type="text"
              value={formData.category}
              onChange={(e) =>
                setFormData({ ...formData, category: e.target.value })
              }
              style={{
                width: "100%",
                padding: "8px",
                marginTop: "5px",
                border: "1px solid #ddd",
                borderRadius: "3px",
              }}
            />
          </div>

          <div>
            <button
              type="submit"
              style={{
                padding: "10px 20px",
                backgroundColor: "#28a745",
                color: "white",
                border: "none",
                borderRadius: "5px",
                cursor: "pointer",
                marginRight: "10px",
              }}
            >
              {editingProduct ? "Update Product" : "Create Product"}
            </button>
            <button
              type="button"
              onClick={resetForm}
              style={{
                padding: "10px 20px",
                backgroundColor: "#6c757d",
                color: "white",
                border: "none",
                borderRadius: "5px",
                cursor: "pointer",
              }}
            >
              Cancel
            </button>
          </div>
        </form>
      )}

      <div>
        <h2>Products List</h2>
        {products.length === 0 ? (
          <p>No products found.</p>
        ) : (
          <div
            style={{
              display: "grid",
              gap: "20px",
              gridTemplateColumns: "repeat(auto-fill, minmax(300px, 1fr))",
            }}
          >
            {products.map((product) => (
              <div
                key={product.id}
                style={{
                  border: "1px solid #ddd",
                  borderRadius: "5px",
                  padding: "15px",
                }}
              >
                <h3>{product.name}</h3>
                <p>{product.description}</p>
                <p>
                  <strong>Price:</strong> ${product.price.toFixed(2)}
                </p>
                <p>
                  <strong>Stock:</strong> {product.stockQuantity}
                </p>
                <p>
                  <strong>Category:</strong> {product.category}
                </p>
                <p>
                  <strong>Status:</strong>{" "}
                  {product.isActive ? "Active" : "Inactive"}
                </p>

                <div style={{ marginTop: "10px" }}>
                  <button
                    onClick={() => startEdit(product)}
                    style={{
                      padding: "5px 10px",
                      backgroundColor: "#ffc107",
                      color: "black",
                      border: "none",
                      borderRadius: "3px",
                      cursor: "pointer",
                      marginRight: "5px",
                    }}
                  >
                    Edit
                  </button>
                  <button
                    onClick={() => handleDelete(product.id)}
                    style={{
                      padding: "5px 10px",
                      backgroundColor: "#dc3545",
                      color: "white",
                      border: "none",
                      borderRadius: "3px",
                      cursor: "pointer",
                    }}
                  >
                    Delete
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

'use client'

import { useState, useEffect } from 'react'
import Link from 'next/link'
import { productsApi, Product } from '@/lib/api'

export default function Home() {
  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    loadProducts()
  }, [])

  const loadProducts = async () => {
    try {
      const data = await productsApi.getAll()
      setProducts(data.slice(0, 6)) // Show only first 6 products on home page
    } catch (err) {
      setError('Failed to load products')
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div>
      <h1>Welcome to the Microservices App</h1>
      
      <nav style={{ marginBottom: '30px' }}>
        <ul style={{ display: 'flex', gap: '20px', listStyle: 'none', padding: 0 }}>
          <li>
            <Link href="/products" style={{ padding: '10px 20px', backgroundColor: '#0070f3', color: 'white', textDecoration: 'none', borderRadius: '5px', display: 'inline-block' }}>
              Manage Products
            </Link>
          </li>
          <li>
            <Link href="/order" style={{ padding: '10px 20px', backgroundColor: '#28a745', color: 'white', textDecoration: 'none', borderRadius: '5px', display: 'inline-block' }}>
              Place an Order
            </Link>
          </li>
          <li>
            <Link href="/billing" style={{ padding: '10px 20px', backgroundColor: '#ffc107', color: 'black', textDecoration: 'none', borderRadius: '5px', display: 'inline-block' }}>
              View Billing
            </Link>
          </li>
        </ul>
      </nav>

      <section>
        <h2>Featured Products</h2>
        {loading && <p>Loading products...</p>}
        {error && <p>Error: {error}</p>}
        {!loading && !error && (
          <>
            {products.length === 0 ? (
              <p>No products available. <Link href="/products">Add some products</Link> to get started.</p>
            ) : (
              <>
                <div style={{ display: 'grid', gap: '20px', gridTemplateColumns: 'repeat(auto-fill, minmax(250px, 1fr))', marginBottom: '20px' }}>
                  {products.map((product) => (
                    <div key={product.id} style={{ border: '1px solid #ddd', borderRadius: '5px', padding: '15px' }}>
                      <h3>{product.name}</h3>
                      <p>{product.description}</p>
                      <p><strong>${product.price.toFixed(2)}</strong></p>
                      <p>Stock: {product.stockQuantity}</p>
                      <p>Category: {product.category}</p>
                    </div>
                  ))}
                </div>
                <Link href="/products" style={{ color: '#0070f3' }}>View all products →</Link>
              </>
            )}
          </>
        )}
      </section>
    </div>
  )
}

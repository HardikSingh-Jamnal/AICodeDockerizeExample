'use client'

import { useState, useEffect } from 'react'
import Link from 'next/link'
import { billingApi, ordersApi, BillingRecord, Order, CreateBillingRequest, getBillingStatusText } from '@/lib/api'

export default function BillingPage() {
  const [billingRecords, setBillingRecords] = useState<BillingRecord[]>([])
  const [orders, setOrders] = useState<Order[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [showForm, setShowForm] = useState(false)
  
  const [formData, setFormData] = useState<CreateBillingRequest>({
    orderId: 0,
    customerId: 1,
    customerName: '',
    customerEmail: '',
    amount: 0,
    taxAmount: 0,
    billingAddress: '',
    paymentMethod: 'Credit Card',
  })

  useEffect(() => {
    loadData()
  }, [])

  const loadData = async () => {
    try {
      setLoading(true)
      const [billingData, ordersData] = await Promise.all([
        billingApi.getAll().catch(() => []),
        ordersApi.getAll().catch(() => [])
      ])
      setBillingRecords(billingData)
      setOrders(ordersData)
    } catch (err) {
      setError('Failed to load data')
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const handleOrderSelect = (orderId: number) => {
    const order = orders.find(o => o.id === orderId)
    if (order) {
      setFormData({
        ...formData,
        orderId: order.id,
        customerId: order.customerId,
        customerName: order.customerName,
        customerEmail: order.customerEmail,
        amount: order.totalAmount,
        taxAmount: order.totalAmount * 0.1, // 10% tax
      })
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    // Check if billing record already exists for this order
    const existingBilling = billingRecords.find(b => b.orderId === formData.orderId)
    if (existingBilling) {
      setError('Billing record already exists for this order')
      return
    }

    try {
      await billingApi.create(formData)
      setSuccess('Billing record created successfully!')
      setShowForm(false)
      setFormData({
        orderId: 0,
        customerId: 1,
        customerName: '',
        customerEmail: '',
        amount: 0,
        taxAmount: 0,
        billingAddress: '',
        paymentMethod: 'Credit Card',
      })
      await loadData()
    } catch (err) {
      setError('Failed to create billing record')
      console.error(err)
    }
  }

  const handleStatusUpdate = async (id: number, status: number) => {
    try {
      const transactionId = status === 1 ? `TXN_${Date.now()}` : undefined // Generate transaction ID for paid status
      await billingApi.updateStatus(id, status, transactionId)
      setSuccess('Billing status updated successfully!')
      await loadData()
    } catch (err) {
      setError('Failed to update billing status')
      console.error(err)
    }
  }

  // Get orders that don't have billing records yet
  const availableOrders = orders.filter(order => 
    !billingRecords.some(billing => billing.orderId === order.id)
  )

  if (loading) return <div>Loading...</div>

  return (
    <div>
      <nav>
        <Link href="/">← Back to Home</Link>
      </nav>
      
      <h1>Billing Management</h1>

      {error && <div style={{ color: 'red', marginBottom: '20px', padding: '10px', border: '1px solid red', borderRadius: '5px' }}>{error}</div>}
      {success && <div style={{ color: 'green', marginBottom: '20px', padding: '10px', border: '1px solid green', borderRadius: '5px' }}>{success}</div>}

      <button 
        onClick={() => setShowForm(!showForm)}
        style={{ marginBottom: '20px', padding: '10px 20px', backgroundColor: '#0070f3', color: 'white', border: 'none', borderRadius: '5px', cursor: 'pointer' }}
        disabled={availableOrders.length === 0}
      >
        {showForm ? 'Cancel' : 'Create New Billing Record'}
      </button>

      {availableOrders.length === 0 && !showForm && (
        <div style={{ marginBottom: '20px', padding: '10px', backgroundColor: '#f8f9fa', border: '1px solid #dee2e6', borderRadius: '5px' }}>
          <p>All orders have billing records. <Link href="/order">Create new orders</Link> to generate more billing records.</p>
        </div>
      )}

      {showForm && (
        <form onSubmit={handleSubmit} style={{ marginBottom: '30px', padding: '20px', border: '1px solid #ddd', borderRadius: '5px' }}>
          <h3>Create Billing Record</h3>
          
          <div style={{ marginBottom: '15px' }}>
            <label>Select Order:</label>
            <select
              value={formData.orderId}
              onChange={(e) => handleOrderSelect(parseInt(e.target.value))}
              required
              style={{ width: '100%', padding: '8px', marginTop: '5px', border: '1px solid #ddd', borderRadius: '3px' }}
            >
              <option value={0}>Select an order...</option>
              {availableOrders.map(order => (
                <option key={order.id} value={order.id}>
                  Order #{order.id} - {order.customerName} - ${order.totalAmount.toFixed(2)}
                </option>
              ))}
            </select>
          </div>

          {formData.orderId > 0 && (
            <>
              <div style={{ marginBottom: '15px' }}>
                <label>Customer Name:</label>
                <input
                  type="text"
                  value={formData.customerName}
                  onChange={(e) => setFormData({ ...formData, customerName: e.target.value })}
                  required
                  style={{ width: '100%', padding: '8px', marginTop: '5px', border: '1px solid #ddd', borderRadius: '3px' }}
                />
              </div>

              <div style={{ marginBottom: '15px' }}>
                <label>Customer Email:</label>
                <input
                  type="email"
                  value={formData.customerEmail}
                  onChange={(e) => setFormData({ ...formData, customerEmail: e.target.value })}
                  required
                  style={{ width: '100%', padding: '8px', marginTop: '5px', border: '1px solid #ddd', borderRadius: '3px' }}
                />
              </div>

              <div style={{ marginBottom: '15px' }}>
                <label>Amount:</label>
                <input
                  type="number"
                  step="0.01"
                  value={formData.amount}
                  onChange={(e) => setFormData({ ...formData, amount: parseFloat(e.target.value) || 0 })}
                  required
                  style={{ width: '100%', padding: '8px', marginTop: '5px', border: '1px solid #ddd', borderRadius: '3px' }}
                />
              </div>

              <div style={{ marginBottom: '15px' }}>
                <label>Tax Amount:</label>
                <input
                  type="number"
                  step="0.01"
                  value={formData.taxAmount}
                  onChange={(e) => setFormData({ ...formData, taxAmount: parseFloat(e.target.value) || 0 })}
                  required
                  style={{ width: '100%', padding: '8px', marginTop: '5px', border: '1px solid #ddd', borderRadius: '3px' }}
                />
              </div>

              <div style={{ marginBottom: '15px' }}>
                <label>Billing Address:</label>
                <textarea
                  value={formData.billingAddress}
                  onChange={(e) => setFormData({ ...formData, billingAddress: e.target.value })}
                  required
                  style={{ width: '100%', padding: '8px', marginTop: '5px', border: '1px solid #ddd', borderRadius: '3px', minHeight: '80px' }}
                />
              </div>

              <div style={{ marginBottom: '15px' }}>
                <label>Payment Method:</label>
                <select
                  value={formData.paymentMethod}
                  onChange={(e) => setFormData({ ...formData, paymentMethod: e.target.value })}
                  required
                  style={{ width: '100%', padding: '8px', marginTop: '5px', border: '1px solid #ddd', borderRadius: '3px' }}
                >
                  <option value="Credit Card">Credit Card</option>
                  <option value="Debit Card">Debit Card</option>
                  <option value="PayPal">PayPal</option>
                  <option value="Bank Transfer">Bank Transfer</option>
                  <option value="Cash">Cash</option>
                </select>
              </div>

              <div style={{ marginBottom: '15px', padding: '10px', backgroundColor: '#f8f9fa', borderRadius: '3px' }}>
                <strong>Total Amount: ${(formData.amount + formData.taxAmount).toFixed(2)}</strong>
              </div>

              <button type="submit" style={{ padding: '10px 20px', backgroundColor: '#28a745', color: 'white', border: 'none', borderRadius: '5px', cursor: 'pointer' }}>
                Create Billing Record
              </button>
            </>
          )}
        </form>
      )}

      <div>
        <h2>Billing Records</h2>
        {billingRecords.length === 0 ? (
          <p>No billing records found.</p>
        ) : (
          <div style={{ display: 'grid', gap: '20px' }}>
            {billingRecords.map((billing) => (
              <div key={billing.id} style={{ border: '1px solid #ddd', borderRadius: '5px', padding: '20px' }}>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px' }}>
                  <div>
                    <h3>Billing Record #{billing.id}</h3>
                    <p><strong>Order ID:</strong> {billing.orderId}</p>
                    <p><strong>Customer:</strong> {billing.customerName}</p>
                    <p><strong>Email:</strong> {billing.customerEmail}</p>
                    <p><strong>Amount:</strong> ${billing.amount.toFixed(2)}</p>
                    <p><strong>Tax:</strong> ${billing.taxAmount.toFixed(2)}</p>
                    <p><strong>Total:</strong> ${billing.totalAmount.toFixed(2)}</p>
                  </div>
                  <div>
                    <p><strong>Status:</strong> <span style={{ 
                      color: billing.status === 1 ? 'green' : billing.status === 2 ? 'red' : billing.status === 3 ? 'orange' : billing.status === 4 ? 'gray' : 'blue' 
                    }}>
                      {getBillingStatusText(billing.status)}
                    </span></p>
                    <p><strong>Payment Method:</strong> {billing.paymentMethod}</p>
                    <p><strong>Billing Date:</strong> {new Date(billing.billingDate).toLocaleDateString()}</p>
                    {billing.paidDate && <p><strong>Paid Date:</strong> {new Date(billing.paidDate).toLocaleDateString()}</p>}
                    {billing.dueDate && <p><strong>Due Date:</strong> {new Date(billing.dueDate).toLocaleDateString()}</p>}
                    {billing.transactionId && <p><strong>Transaction ID:</strong> {billing.transactionId}</p>}
                    
                    <div style={{ marginTop: '15px' }}>
                      <label style={{ display: 'block', marginBottom: '5px' }}>Update Status:</label>
                      <select
                        onChange={(e) => handleStatusUpdate(billing.id, parseInt(e.target.value))}
                        style={{ padding: '5px', borderRadius: '3px', border: '1px solid #ddd' }}
                      >
                        <option value="">Select status...</option>
                        <option value={0}>Pending</option>
                        <option value={1}>Paid</option>
                        <option value={2}>Failed</option>
                        <option value={3}>Refunded</option>
                        <option value={4}>Cancelled</option>
                      </select>
                    </div>
                  </div>
                </div>
                <div style={{ marginTop: '15px', paddingTop: '15px', borderTop: '1px solid #eee' }}>
                  <p><strong>Billing Address:</strong></p>
                  <p style={{ whiteSpace: 'pre-wrap' }}>{billing.billingAddress}</p>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

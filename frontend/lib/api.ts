// API Base Configuration
const API_BASE_URLS = {
  products: process.env.NEXT_PUBLIC_PRODUCTS_API || 'http://localhost:5001',
  orders: process.env.NEXT_PUBLIC_ORDERS_API || 'http://localhost:5002',
  billing: process.env.NEXT_PUBLIC_BILLING_API || 'http://localhost:5003',
}

// Types
export interface Product {
  id: number
  name: string
  description: string
  price: number
  stockQuantity: number
  category: string
  isActive: boolean
}

export interface CreateProductRequest {
  name: string
  description: string
  price: number
  stockQuantity: number
  category: string
}

export interface UpdateProductRequest extends CreateProductRequest {
  id: number
}

export interface OrderItem {
  productId: number
  productName: string
  unitPrice: number
  quantity: number
}

export interface Order {
  id: number
  customerId: number
  customerName: string
  customerEmail: string
  totalAmount: number
  status: number
  shippingAddress: string
  orderDate: string
  shippedDate?: string
  deliveredDate?: string
}

export interface CreateOrderRequest {
  customerId: number
  customerName: string
  customerEmail: string
  shippingAddress: string
  orderItems: OrderItem[]
}

export interface BillingRecord {
  id: number
  orderId: number
  customerId: number
  customerName: string
  customerEmail: string
  amount: number
  taxAmount: number
  totalAmount: number
  status: number
  billingAddress: string
  paymentMethod: string
  transactionId: string
  billingDate: string
  paidDate?: string
  dueDate?: string
}

export interface CreateBillingRequest {
  orderId: number
  customerId: number
  customerName: string
  customerEmail: string
  amount: number
  taxAmount: number
  billingAddress: string
  paymentMethod: string
}

// API Functions
async function apiRequest(url: string, options: RequestInit = {}) {
  const response = await fetch(url, {
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
    ...options,
  })

  if (!response.ok) {
    throw new Error(`API Error: ${response.status} ${response.statusText}`)
  }

  return response.json()
}

// Products API
export const productsApi = {
  getAll: async (): Promise<Product[]> => {
    return apiRequest(`${API_BASE_URLS.products}/products`)
  },

  getById: async (id: number): Promise<Product> => {
    return apiRequest(`${API_BASE_URLS.products}/products/${id}`)
  },

  create: async (product: CreateProductRequest): Promise<{ id: number }> => {
    return apiRequest(`${API_BASE_URLS.products}/products`, {
      method: 'POST',
      body: JSON.stringify(product),
    })
  },

  update: async (id: number, product: CreateProductRequest): Promise<void> => {
    await apiRequest(`${API_BASE_URLS.products}/products/${id}`, {
      method: 'PUT',
      body: JSON.stringify({ ...product, id }),
    })
  },

  delete: async (id: number): Promise<void> => {
    await apiRequest(`${API_BASE_URLS.products}/products/${id}`, {
      method: 'DELETE',
    })
  },
}

// Orders API
export const ordersApi = {
  getAll: async (): Promise<Order[]> => {
    return apiRequest(`${API_BASE_URLS.orders}/orders`)
  },

  create: async (order: CreateOrderRequest): Promise<{ id: number }> => {
    return apiRequest(`${API_BASE_URLS.orders}/orders`, {
      method: 'POST',
      body: JSON.stringify(order),
    })
  },
}

// Billing API
export const billingApi = {
  getAll: async (): Promise<BillingRecord[]> => {
    return apiRequest(`${API_BASE_URLS.billing}/billing`)
  },

  create: async (billing: CreateBillingRequest): Promise<{ id: number }> => {
    return apiRequest(`${API_BASE_URLS.billing}/billing`, {
      method: 'POST',
      body: JSON.stringify(billing),
    })
  },

  updateStatus: async (id: number, status: number, transactionId?: string): Promise<void> => {
    await apiRequest(`${API_BASE_URLS.billing}/billing/${id}/status`, {
      method: 'PUT',
      body: JSON.stringify({ id, status, transactionId }),
    })
  },
}

// Helper functions
export const getOrderStatusText = (status: number): string => {
  const statuses = ['Pending', 'Confirmed', 'Shipped', 'Delivered', 'Cancelled']
  return statuses[status] || 'Unknown'
}

export const getBillingStatusText = (status: number): string => {
  const statuses = ['Pending', 'Paid', 'Failed', 'Refunded', 'Cancelled']
  return statuses[status] || 'Unknown'
}
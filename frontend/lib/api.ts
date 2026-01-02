// API Base Configuration
const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5000'

// Types for Search
export interface Offer {
  id: string
  vin: string
  make: string
  model: string
  year: number
  price: string
  owner: string
}

export interface Purchase {
  id: string
  offerId: string
  buyer: string
  status: string
}

export interface Transport {
  id: string
  vehicle: string
  carrier: string
  status: string
}

export interface SearchResults {
  offers: Offer[]
  purchases: Purchase[]
  transports: Transport[]
}

export interface SearchParams {
  accountId: string
  query?: string
}

// API Functions
async function apiRequest<T>(url: string, options: RequestInit = {}): Promise<T> {
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

// Search API
export const searchApi = {
  /**
   * Search across all entities (offers, purchases, transports)
   * @param params - Search parameters including accountId and query
   * @returns Search results containing matching offers, purchases, and transports
   */
  search: async (params: SearchParams): Promise<SearchResults> => {
    const queryParams = new URLSearchParams()
    
    queryParams.append('accountId', params.accountId)
    
    if (params.query) {
      queryParams.append('q', params.query)
    }

    const url = `${API_BASE_URL}/search?${queryParams.toString()}`
    
    return apiRequest<SearchResults>(url)
  },
}

// Mock data for development
const mockData: SearchResults = {
  offers: [
    { id: 'O-123', vin: '1HGCM82633A004352', make: 'Toyota', model: 'Camry', year: 2022, price: '$22,000', owner: 'Seller A' },
    { id: 'O-124', vin: '2HGCM82633A004353', make: 'Honda', model: 'Accord', year: 2023, price: '$25,500', owner: 'Seller B' },
  ],
  purchases: [
    { id: 'P-456', offerId: 'O-123', buyer: 'Buyer X', status: 'Completed' },
    { id: 'P-457', offerId: 'O-124', buyer: 'Buyer Y', status: 'Pending' },
  ],
  transports: [
    { id: 'T-789', vehicle: 'Camry 2022', carrier: 'Carrier Y', status: 'In Transit' },
    { id: 'T-790', vehicle: 'Accord 2023', carrier: 'Carrier Z', status: 'Delivered' },
  ],
}

// Service layer - currently uses mock data, will switch to API when ready
const USE_MOCK = process.env.NEXT_PUBLIC_USE_MOCK_API !== 'false'

export const searchService = {
  /**
   * Search with accountId and optional query
   * Currently returns mock data, will call API when ready
   */
  search: async (params: SearchParams): Promise<SearchResults> => {
    if (USE_MOCK) {
      // Simulate API delay
      await new Promise(resolve => setTimeout(resolve, 150))
      return mockData
    }
    return searchApi.search(params)
  },
}

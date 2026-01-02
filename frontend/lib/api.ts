// Autocomplete Suggestion Type
export interface AutocompleteSuggestion {
  text: string;
  entityType: string;
  entityId: string;
  score: number;
}
// API Base Configuration
const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5007'

// Types for Search
export interface Offer {
  id: string
  vin: string
  make: string
  model: string
  year: number
  price: string
  city: string
  state: string
  status: string
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
  sellerId?: string;
  buyerId?: string;
  carrierId?: string;
  query?: string;
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
     * Autocomplete API
     * @param query - The search query string
     * @returns Array of autocomplete suggestions
     */
    autocomplete: async (query: string): Promise<AutocompleteSuggestion[]> => {
      const url = `${API_BASE_URL}/api/search/autocomplete?q=${encodeURIComponent(query)}`;
      return apiRequest<AutocompleteSuggestion[]>(url);
    },
  /**
   * Search across all entities (offers, purchases, transports)
   * @param params - Search parameters including accountId and query
   * @returns Search results containing matching offers, purchases, and transports
   */
  search: async (params: SearchParams): Promise<SearchResults> => {
    const queryParams = new URLSearchParams();
    if (params.sellerId) queryParams.append('sellerId', params.sellerId);
    if (params.buyerId) queryParams.append('buyerId', params.buyerId);
    if (params.carrierId) queryParams.append('carrierId', params.carrierId);
    if (params.query) queryParams.append('q', params.query);
    const url = `${API_BASE_URL}/api/search?${queryParams.toString()}`;
    return apiRequest<SearchResults>(url);
  },
}

// Service layer - currently uses mock data, will switch to API when ready
const USE_MOCK = process.env.NEXT_PUBLIC_USE_MOCK_API !== 'false'

export const searchService = {
    /**
     * Get autocomplete suggestions for a query
     */
    autocomplete: async (query: string): Promise<AutocompleteSuggestion[]> => {
      return searchApi.autocomplete(query);
    },
  /**
   * Search with accountId and optional query
   * Maps backend 'documents' array to frontend SearchResults format
   */
  search: async (params: SearchParams): Promise<SearchResults> => {
    const backendResult = await searchApi.search(params) as SearchResults & { documents?: any[] };
    // If backend returns 'documents', map to offers, purchases, transports
    if (backendResult && Array.isArray((backendResult as any).documents)) {
      const docs = (backendResult as any).documents;
      // entityType: 0 = Offer, 1 = Purchase, 2 = Transport
      const offers = docs.filter((doc: any) => doc.entityType === 0).map((doc: any) => ({
        id: doc.id,
        vin: doc.vin,
        make: doc.make,
        model: doc.model,
        year: doc.year,
        price: doc.amount ? `$${doc.amount.toLocaleString()}` : '',
        city: doc.city || '',
        state: doc.state || '',
        status: doc.status || '',
        owner: doc.sellerId || '',
      }));
      const purchases = docs.filter((doc: any) => doc.entityType === 1).map((doc: any) => ({
        id: doc.id,
        offerId: doc.offerId || '',
        buyer: doc.buyerId || '',
        status: doc.status || '',
      }));
      const transports = docs.filter((doc: any) => doc.entityType === 2).map((doc: any) => ({
        id: doc.id,
        vehicle: doc.vehicle || '',
        carrier: doc.carrierId || '',
        status: doc.status || '',
      }));
      return { offers, purchases, transports };
    }
    // Fallback to original format if already correct
    return backendResult;
  },
};

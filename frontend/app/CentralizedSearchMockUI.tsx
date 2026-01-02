import React, { useState, useRef, useEffect, useMemo } from "react";
import { Search, Package, ShoppingCart, Truck, User, X } from "lucide-react";

// Levenshtein distance for typo tolerance
function levenshteinDistance(a: string, b: string): number {
  const matrix: number[][] = [];
  
  if (a.length === 0) return b.length;
  if (b.length === 0) return a.length;

  for (let i = 0; i <= b.length; i++) {
    matrix[i] = [i];
  }
  for (let j = 0; j <= a.length; j++) {
    matrix[0][j] = j;
  }

  for (let i = 1; i <= b.length; i++) {
    for (let j = 1; j <= a.length; j++) {
      if (b.charAt(i - 1) === a.charAt(j - 1)) {
        matrix[i][j] = matrix[i - 1][j - 1];
      } else {
        matrix[i][j] = Math.min(
          matrix[i - 1][j - 1] + 1, // substitution
          matrix[i][j - 1] + 1,     // insertion
          matrix[i - 1][j] + 1      // deletion
        );
      }
    }
  }

  return matrix[b.length][a.length];
}

// Fuzzy match with typo tolerance
function fuzzyMatch(query: string, target: string, threshold: number = 0.3): boolean {
  const q = query.toLowerCase().trim();
  const t = target.toLowerCase();
  
  if (q === "") return true;
  if (t.includes(q)) return true; // exact substring match
  
  // Split target into words and check each
  const words = t.split(/\s+/);
  for (const word of words) {
    // Allow more tolerance for longer words
    const maxDistance = Math.max(1, Math.floor(word.length * threshold));
    const distance = levenshteinDistance(q, word);
    if (distance <= maxDistance) return true;
    
    // Also check if query is a fuzzy prefix of the word
    if (q.length <= word.length) {
      const prefix = word.substring(0, q.length);
      const prefixDistance = levenshteinDistance(q, prefix);
      if (prefixDistance <= Math.max(1, Math.floor(q.length * threshold))) return true;
    }
  }
  
  // Check against the full target string for longer queries
  if (q.length >= 3) {
    const maxDistance = Math.max(1, Math.floor(q.length * threshold));
    const distance = levenshteinDistance(q, t);
    if (distance <= maxDistance) return true;
  }
  
  return false;
}


interface Offer {
  id: string;
  vin: string;
  make: string;
  model: string;
  year: number;
  price: string;
  owner: string;
}

interface Purchase {
  id: string;
  offerId: string;
  buyer: string;
  status: string;
}

interface Transport {
  id: string;
  vehicle: string;
  carrier: string;
  status: string;
}

const mockResults = {
  offers: [
    { id: "O-123", vin: "1HGCM82633A004352", make: "Toyota", model: "Camry", year: 2022, price: "$22,000", owner: "Seller A" },
    { id: "O-124", vin: "2HGCM82633A004353", make: "Honda", model: "Accord", year: 2023, price: "$25,500", owner: "Seller B" },
  ],
  purchases: [
    { id: "P-456", offerId: "O-123", buyer: "Buyer X", status: "Completed" },
    { id: "P-457", offerId: "O-124", buyer: "Buyer Y", status: "Pending" },
  ],
  transports: [
    { id: "T-789", vehicle: "Camry 2022", carrier: "Carrier Y", status: "In Transit" },
    { id: "T-790", vehicle: "Accord 2023", carrier: "Carrier Z", status: "Delivered" },
  ],
};

export default function CentralizedSearchMock() {
  const [query, setQuery] = useState("");
  const [role, setRole] = useState("Agent");
  const [activeTab, setActiveTab] = useState("all");
  const [accountId, setAccountId] = useState("");
  
  // Search autocomplete state
  const [showSearchDropdown, setShowSearchDropdown] = useState(false);
  const [searchHighlightedIndex, setSearchHighlightedIndex] = useState(-1);
  const searchInputRef = useRef<HTMLInputElement>(null);
  const searchDropdownRef = useRef<HTMLDivElement>(null);

  const filteredResults = useMemo(() => ({
    offers: mockResults.offers.filter((o: Offer) =>
      query === "" ||
      fuzzyMatch(query, o.make) ||
      fuzzyMatch(query, o.model) ||
      fuzzyMatch(query, o.vin) ||
      fuzzyMatch(query, `${o.year}`) ||
      fuzzyMatch(query, o.owner)
    ),
    purchases: mockResults.purchases.filter((p: Purchase) =>
      query === "" ||
      fuzzyMatch(query, p.id) ||
      fuzzyMatch(query, p.buyer) ||
      fuzzyMatch(query, p.status)
    ),
    transports: mockResults.transports.filter((t: Transport) =>
      query === "" ||
      fuzzyMatch(query, t.id) ||
      fuzzyMatch(query, t.vehicle) ||
      fuzzyMatch(query, t.carrier) ||
      fuzzyMatch(query, t.status)
    ),
  }), [query]);

  const tabs = [
    { id: "all", label: "All Results", icon: Search },
    { id: "offers", label: "Offers", icon: Package },
    { id: "purchases", label: "Purchases", icon: ShoppingCart },
    { id: "transports", label: "Transports", icon: Truck },
  ];

  const showSection = (section: string) => activeTab === "all" || activeTab === section;

  // Generate search suggestions
  const getSearchSuggestions = (): string[] => {
    if (!query || query.trim().length === 0) return [];
    
    const queryLower = query.toLowerCase().trim();
    const suggestions = new Set<string>();
    
    // Add offers suggestions (VIN, make, model)
    mockResults.offers.forEach((o: Offer) => {
      if (o.vin.toLowerCase().includes(queryLower)) suggestions.add(o.vin);
      if (o.make.toLowerCase().includes(queryLower)) suggestions.add(o.make);
      if (o.model.toLowerCase().includes(queryLower)) suggestions.add(o.model);
      if (o.id.toLowerCase().includes(queryLower)) suggestions.add(o.id);
      if (o.owner.toLowerCase().includes(queryLower)) suggestions.add(o.owner);
    });
    
    // Add purchases suggestions (ID, buyer, offerId)
    mockResults.purchases.forEach((p: Purchase) => {
      if (p.id.toLowerCase().includes(queryLower)) suggestions.add(p.id);
      if (p.buyer.toLowerCase().includes(queryLower)) suggestions.add(p.buyer);
      if (p.offerId.toLowerCase().includes(queryLower)) suggestions.add(p.offerId);
    });
    
    // Add transports suggestions (ID, vehicle, carrier)
    mockResults.transports.forEach((t: Transport) => {
      if (t.id.toLowerCase().includes(queryLower)) suggestions.add(t.id);
      if (t.vehicle.toLowerCase().includes(queryLower)) suggestions.add(t.vehicle);
      if (t.carrier.toLowerCase().includes(queryLower)) suggestions.add(t.carrier);
    });
    
    return Array.from(suggestions).slice(0, 10); // Limit to 10 suggestions
  };

  const searchSuggestions = getSearchSuggestions();

  // Clear account ID
  const handleClearAccountId = () => {
    setAccountId("");
  };

  // Handle keyboard navigation for search input
  const handleSearchKeyDown = (e: React.KeyboardEvent) => {
    if (!showSearchDropdown || searchSuggestions.length === 0) {
      if (e.key === "ArrowDown") {
        setShowSearchDropdown(true);
        setSearchHighlightedIndex(0);
      }
      return;
    }

    switch (e.key) {
      case "ArrowDown":
        e.preventDefault();
        setSearchHighlightedIndex((prev) =>
          prev < searchSuggestions.length - 1 ? prev + 1 : prev
        );
        break;
      case "ArrowUp":
        e.preventDefault();
        setSearchHighlightedIndex((prev) => (prev > 0 ? prev - 1 : 0));
        break;
      case "Enter":
        e.preventDefault();
        if (searchHighlightedIndex >= 0 && searchHighlightedIndex < searchSuggestions.length) {
          setQuery(searchSuggestions[searchHighlightedIndex]);
          setShowSearchDropdown(false);
          setSearchHighlightedIndex(-1);
        }
        break;
      case "Escape":
        e.preventDefault();
        setShowSearchDropdown(false);
        setSearchHighlightedIndex(-1);
        break;
    }
  };

  // Handle search suggestion selection
  const handleSearchSuggestionSelect = (suggestion: string) => {
    setQuery(suggestion);
    setShowSearchDropdown(false);
    setSearchHighlightedIndex(-1);
    searchInputRef.current?.focus();
  };

  // Scroll highlighted search suggestion into view
  useEffect(() => {
    if (searchHighlightedIndex >= 0 && searchDropdownRef.current) {
      const items = searchDropdownRef.current.querySelectorAll("li");
      items[searchHighlightedIndex]?.scrollIntoView({ block: "nearest" });
    }
  }, [searchHighlightedIndex]);

  // Close search dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (
        searchInputRef.current &&
        !searchInputRef.current.contains(e.target as Node) &&
        searchDropdownRef.current &&
        !searchDropdownRef.current.contains(e.target as Node)
      ) {
        setShowSearchDropdown(false);
        setSearchHighlightedIndex(-1);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  // Highlight matching text
  const highlightMatch = (text: string, query: string) => {
    if (!query) return text;
    const index = text.toLowerCase().indexOf(query.toLowerCase());
    if (index === -1) return text;

    return (
      <>
        {text.substring(0, index)}
        <span className="bg-yellow-200 font-semibold">
          {text.substring(index, index + query.length)}
        </span>
        {text.substring(index + query.length)}
      </>
    );
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100">
      {/* Header */}
      <div className="bg-white border-b border-slate-200 shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 py-4 sm:py-6">
          <div className="flex items-center justify-between mb-4 sm:mb-6">
            <div>
              <h1 className="text-2xl sm:text-3xl font-bold text-slate-900">AI Challenge</h1>
            </div>
          </div>

          {/* Account Type, Account Id and Search Bar - Responsive Layout */}
          <div className="relative mb-4 sm:mb-6">
            <div className="flex flex-col lg:flex-row lg:items-center gap-3 lg:gap-4">
              {/* Account Type and Account Id - Row on mobile, inline on desktop */}
              <div className="flex flex-col sm:flex-row gap-3 sm:gap-0 sm:items-stretch">
                {/* Account Type Dropdown */}
                <div className="flex items-center gap-2 sm:gap-3 bg-slate-100 px-3 sm:px-4 h-11 rounded-lg sm:rounded-r-none border-2 border-transparent">
                  <User className="w-5 h-5 text-slate-600 flex-shrink-0" />
                  <select
                    className="bg-transparent border-none outline-none font-medium text-slate-900 cursor-pointer text-sm sm:text-base flex-1 min-w-0"
                    value={role}
                    onChange={(e) => setRole(e.target.value)}
                  >
                    <option>Seller</option>
                    <option>Buyer</option>
                    <option>Carrier</option>
                    <option>Agent</option>
                  </select>
                </div>

                {/* Account Id Text Input */}
                <div className="relative flex-1 sm:flex-initial">
                  <input
                    type="text"
                    className="w-full sm:w-48 px-3 pr-8 h-11 rounded-lg sm:rounded-l-none sm:rounded-r-lg border-2 border-slate-200 sm:border-l-0 focus:border-blue-500 focus:ring-2 focus:ring-blue-200 outline-none transition-all bg-white text-sm sm:text-base"
                    placeholder="Account ID"
                    value={accountId}
                    onChange={(e) => setAccountId(e.target.value)}
                    autoComplete="off"
                  />
                  {accountId && (
                    <button
                      type="button"
                      className="absolute right-2 top-1/2 -translate-y-1/2 p-1 rounded hover:bg-slate-200 transition-colors"
                      onClick={handleClearAccountId}
                      tabIndex={-1}
                      aria-label="Clear account id"
                    >
                      <X className="w-4 h-4 text-slate-500" />
                    </button>
                  )}
                </div>
              </div>

              {/* Search Bar */}
              <div className="relative flex-1">
                <Search className="absolute left-3 sm:left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-slate-400 z-10" />
                <input
                  ref={searchInputRef}
                  className="w-full pl-10 sm:pl-12 pr-4 h-11 rounded-lg bg-slate-50 border-2 border-slate-200 focus:border-blue-500 focus:bg-white transition-all outline-none text-slate-900 placeholder-slate-400 text-sm sm:text-base"
                  placeholder="Search by VIN, vehicle, buyer, or ID..."
                  value={query}
                  onChange={(e) => {
                    setQuery(e.target.value);
                    setShowSearchDropdown(true);
                    setSearchHighlightedIndex(-1);
                  }}
                  onFocus={() => {
                    if (query && searchSuggestions.length > 0) {
                      setShowSearchDropdown(true);
                    }
                  }}
                  onKeyDown={handleSearchKeyDown}
                  autoComplete="off"
                />
                
                {/* Search Autocomplete Dropdown */}
                {showSearchDropdown && searchSuggestions.length > 0 && (
                  <div
                    ref={searchDropdownRef}
                    className="absolute z-30 left-0 right-0 mt-2 bg-white border border-slate-200 rounded-lg shadow-lg max-h-60 overflow-auto"
                  >
                    <ul>
                      {searchSuggestions.map((suggestion, index) => (
                        <li
                          key={suggestion}
                          className={`px-4 py-2.5 cursor-pointer transition-colors text-sm sm:text-base ${
                            index === searchHighlightedIndex
                              ? "bg-blue-100 text-blue-900"
                              : "hover:bg-slate-50"
                          }`}
                          onMouseDown={() => handleSearchSuggestionSelect(suggestion)}
                          onMouseEnter={() => setSearchHighlightedIndex(index)}
                        >
                          {highlightMatch(suggestion, query)}
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="bg-white border-b border-slate-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6">
          <div className="flex gap-1 overflow-x-auto scrollbar-hide -mx-4 sm:mx-0 px-4 sm:px-0">
            {tabs.map((tab) => {
              const Icon = tab.icon;
              const isActive = activeTab === tab.id;
              return (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`flex items-center gap-1.5 sm:gap-2 px-3 sm:px-6 py-3 sm:py-4 text-sm sm:text-base font-medium transition-all border-b-2 whitespace-nowrap ${
                    isActive
                      ? "text-blue-600 border-blue-600"
                      : "text-slate-600 border-transparent hover:text-slate-900 hover:bg-slate-50"
                  }`}
                >
                  <Icon className="w-4 h-4 flex-shrink-0" />
                  <span className="hidden sm:inline">{tab.label}</span>
                  <span className="sm:hidden">{tab.id === "all" ? "All" : tab.label.split(" ")[0]}</span>
                </button>
              );
            })}
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 py-4 sm:py-8">
        <div className="space-y-8">
          {showSection("offers") && (
            <Section
              title="Offers"
              icon={Package}
              items={filteredResults.offers}
              render={(o: Offer) => (
                <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-2">
                  <div className="flex-1 min-w-0">
                    <div className="flex flex-wrap items-center gap-2 sm:gap-3 mb-2">
                      <span className="text-base sm:text-lg font-bold text-slate-900">
                        {o.year} {o.make} {o.model}
                      </span>
                      <span className="px-2 py-1 bg-emerald-100 text-emerald-700 text-xs font-semibold rounded-full">
                        {o.price}
                      </span>
                    </div>
                    <div className="text-xs sm:text-sm text-slate-600 space-y-1">
                      <div>Offer ID: <span className="font-mono text-slate-900">{o.id}</span></div>
                      <div className="break-all">VIN: <span className="font-mono text-slate-900">{o.vin}</span></div>
                      <div>Owner: <span className="font-medium text-slate-900">{o.owner}</span></div>
                    </div>
                  </div>
                </div>
              )}
            />
          )}

          {showSection("purchases") && (
            <Section
              title="Purchases"
              icon={ShoppingCart}
              items={filteredResults.purchases}
              render={(p: Purchase) => (
                <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-2">
                  <div className="flex-1 min-w-0">
                    <div className="flex flex-wrap items-center gap-2 sm:gap-3 mb-2">
                      <span className="text-base sm:text-lg font-bold text-slate-900">{p.id}</span>
                      <span
                        className={`px-2 py-1 text-xs font-semibold rounded-full ${
                          p.status === "Completed"
                            ? "bg-emerald-100 text-emerald-700"
                            : "bg-amber-100 text-amber-700"
                        }`}
                      >
                        {p.status}
                      </span>
                    </div>
                    <div className="text-xs sm:text-sm text-slate-600 space-y-1">
                      <div>Offer ID: <span className="font-mono text-slate-900">{p.offerId}</span></div>
                      <div>Buyer: <span className="font-medium text-slate-900">{p.buyer}</span></div>
                    </div>
                  </div>
                </div>
              )}
            />
          )}

          {showSection("transports") && (
            <Section
              title="Transports"
              icon={Truck}
              items={filteredResults.transports}
              render={(t: Transport) => (
                <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-2">
                  <div className="flex-1 min-w-0">
                    <div className="flex flex-wrap items-center gap-2 sm:gap-3 mb-2">
                      <span className="text-base sm:text-lg font-bold text-slate-900">{t.id}</span>
                      <span
                        className={`px-2 py-1 text-xs font-semibold rounded-full ${
                          t.status === "Delivered"
                            ? "bg-emerald-100 text-emerald-700"
                            : "bg-blue-100 text-blue-700"
                        }`}
                      >
                        {t.status}
                      </span>
                    </div>
                    <div className="text-xs sm:text-sm text-slate-600 space-y-1">
                      <div>Vehicle: <span className="font-medium text-slate-900">{t.vehicle}</span></div>
                      <div>Carrier: <span className="font-medium text-slate-900">{t.carrier}</span></div>
                    </div>
                  </div>
                </div>
              )}
            />
          )}

          {filteredResults.offers.length === 0 &&
            filteredResults.purchases.length === 0 &&
            filteredResults.transports.length === 0 && (
              <div className="text-center py-12 sm:py-16 px-4">
                <div className="inline-flex items-center justify-center w-12 h-12 sm:w-16 sm:h-16 bg-slate-100 rounded-full mb-4">
                  <Search className="w-6 h-6 sm:w-8 sm:h-8 text-slate-400" />
                </div>
                <h3 className="text-lg sm:text-xl font-semibold text-slate-900 mb-2">No results found</h3>
                <p className="text-sm sm:text-base text-slate-600">Try adjusting your search terms</p>
              </div>
            )}
        </div>
      </div>
    </div>
  );
}

function Section<T>({
  title,
  icon: Icon,
  items,
  render,
}: {
  title: string;
  icon: React.ComponentType<{ className?: string }>;
  items: T[];
  render: (item: T) => React.ReactNode;
}) {
  if (items.length === 0) return null;

  return (
    <div>
      <div className="flex items-center gap-2 mb-3 sm:mb-4">
        <Icon className="w-4 h-4 sm:w-5 sm:h-5 text-slate-600" />
        <h2 className="text-lg sm:text-xl font-bold text-slate-900">{title}</h2>
        <span className="px-2 py-0.5 bg-slate-200 text-slate-700 text-xs font-semibold rounded-full">
          {items.length}
        </span>
      </div>
      <div className="grid gap-3 sm:gap-4">
        {items.map((item, idx) => (
          <div
            key={idx}
            className="p-4 sm:p-6 rounded-lg sm:rounded-xl bg-white border border-slate-200 hover:border-slate-300 hover:shadow-md transition-all"
          >
            {render(item)}
          </div>
        ))}
      </div>
    </div>
  );
}
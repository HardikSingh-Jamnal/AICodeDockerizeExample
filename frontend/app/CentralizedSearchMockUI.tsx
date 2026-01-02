import React, { useState, useRef, useEffect, useMemo } from "react";
import { Search, Package, ShoppingCart, Truck, User, ChevronDown, ChevronUp, X } from "lucide-react";

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

// Mock account ids for each role
const accountIdsByRole: Record<string, string[]> = {
  Seller: ["seller-001", "seller-002", "seller-003", "seller-xyz"],
  Buyer: ["buyer-001", "buyer-002", "buyer-abc", "buyer-xyz"],
  Carrier: ["carrier-001", "carrier-002", "carrier-xyz"],
  Agent: ["agent-001", "agent-002", "agent-xyz"],
};

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
  const [accountInput, setAccountInput] = useState("");
  const [selectedAccount, setSelectedAccount] = useState<string | null>(null);
  const [showDropdown, setShowDropdown] = useState(false);
  const [highlightedIndex, setHighlightedIndex] = useState(-1);
  const inputRef = useRef<HTMLInputElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  
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

  // Filter account ids based on input
  const filteredAccounts = accountIdsByRole[role].filter((id) =>
    id.toLowerCase().includes(accountInput.toLowerCase())
  );

  // Handle account selection
  const handleAccountSelect = (id: string) => {
    setSelectedAccount(id);
    setAccountInput(id);
    setShowDropdown(false);
    setHighlightedIndex(-1);
  };

  // Clear selection
  const handleClear = () => {
    setAccountInput("");
    setSelectedAccount(null);
    setShowDropdown(false);
    setHighlightedIndex(-1);
    inputRef.current?.focus();
  };

  // Reset account input when role changes
  useEffect(() => {
    setAccountInput("");
    setSelectedAccount(null);
    setShowDropdown(false);
    setHighlightedIndex(-1);
  }, [role]);

  // Handle keyboard navigation for account input
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (!showDropdown || filteredAccounts.length === 0) {
      if (e.key === "ArrowDown") {
        setShowDropdown(true);
        setHighlightedIndex(0);
      }
      return;
    }

    switch (e.key) {
      case "ArrowDown":
        e.preventDefault();
        setHighlightedIndex((prev) =>
          prev < filteredAccounts.length - 1 ? prev + 1 : prev
        );
        break;
      case "ArrowUp":
        e.preventDefault();
        setHighlightedIndex((prev) => (prev > 0 ? prev - 1 : 0));
        break;
      case "Enter":
        e.preventDefault();
        if (highlightedIndex >= 0 && highlightedIndex < filteredAccounts.length) {
          handleAccountSelect(filteredAccounts[highlightedIndex]);
        }
        break;
      case "Escape":
        e.preventDefault();
        setShowDropdown(false);
        setHighlightedIndex(-1);
        break;
    }
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

  // Scroll highlighted item into view
  useEffect(() => {
    if (highlightedIndex >= 0 && dropdownRef.current) {
      const items = dropdownRef.current.querySelectorAll("li");
      items[highlightedIndex]?.scrollIntoView({ block: "nearest" });
    }
  }, [highlightedIndex]);

  // Scroll highlighted search suggestion into view
  useEffect(() => {
    if (searchHighlightedIndex >= 0 && searchDropdownRef.current) {
      const items = searchDropdownRef.current.querySelectorAll("li");
      items[searchHighlightedIndex]?.scrollIntoView({ block: "nearest" });
    }
  }, [searchHighlightedIndex]);

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      // Handle account dropdown
      if (
        inputRef.current &&
        !inputRef.current.contains(e.target as Node) &&
        dropdownRef.current &&
        !dropdownRef.current.contains(e.target as Node)
      ) {
        setShowDropdown(false);
        setHighlightedIndex(-1);
      }
      
      // Handle search dropdown
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
        <div className="max-w-7xl mx-auto px-6 py-6">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h1 className="text-3xl font-bold text-slate-900">AI Challenge</h1>
            </div>
          </div>

          {/* Account Type, Account Id dropdowns and Search Bar */}
          <div className="relative mb-6">
            <div className="flex items-center gap-4">
              {/* Account Type Dropdown with nested Account Id */}
              <div className="flex items-center gap-3 bg-slate-100 px-4 py-2 rounded-lg">
                <User className="w-5 h-5 text-slate-600" />
                <select
                  className="bg-transparent border-none outline-none font-medium text-slate-900 cursor-pointer"
                  value={role}
                  onChange={(e) => setRole(e.target.value)}
                >
                  <option>Seller</option>
                  <option>Buyer</option>
                  <option>Carrier</option>
                  <option>Agent</option>
                </select>

                {/* Vertical divider */}
                <div className="w-px h-6 bg-slate-300"></div>

                {/* Account Id Typeahead */}
                <div className="relative">
                  <input
                    ref={inputRef}
                    type="text"
                    className="px-3 py-1.5 pr-16 rounded-md border border-slate-300 focus:border-blue-500 focus:ring-2 focus:ring-blue-200 outline-none w-48 transition-all bg-white"
                    placeholder="Account ID"
                    value={accountInput}
                    onChange={(e) => {
                      setAccountInput(e.target.value);
                      setSelectedAccount(null);
                      setShowDropdown(true);
                      setHighlightedIndex(-1);
                    }}
                    onFocus={() => setShowDropdown(true)}
                    onKeyDown={handleKeyDown}
                    autoComplete="off"
                  />
                  <div className="absolute right-2 top-1/2 -translate-y-1/2 flex items-center gap-1">
                    {selectedAccount && (
                      <button
                        type="button"
                        className="p-1 rounded hover:bg-slate-200 transition-colors"
                        onClick={handleClear}
                        tabIndex={-1}
                        aria-label="Clear account id"
                      >
                        <X className="w-4 h-4 text-slate-500" />
                      </button>
                    )}
                    <button
                      type="button"
                      className="p-1 rounded hover:bg-slate-200 transition-colors"
                      onClick={() => setShowDropdown(!showDropdown)}
                      tabIndex={-1}
                      aria-label={showDropdown ? "Close account id dropdown" : "Open account id dropdown"}
                    >
                      {showDropdown ? (
                        <ChevronUp className="w-4 h-4 text-slate-600" />
                      ) : (
                        <ChevronDown className="w-4 h-4 text-slate-600" />
                      )}
                    </button>
                  </div>
                  
                  {showDropdown && (
                    <div
                      ref={dropdownRef}
                      className="absolute z-20 left-0 w-full mt-2 bg-white border border-slate-200 rounded-lg shadow-lg max-h-60 overflow-auto"
                    >
                      {filteredAccounts.length > 0 ? (
                        <ul>
                          {filteredAccounts.map((id, index) => (
                            <li
                              key={id}
                              className={`px-4 py-2.5 cursor-pointer transition-colors ${
                                index === highlightedIndex
                                  ? "bg-blue-100 text-blue-900"
                                  : "hover:bg-slate-50"
                              } ${selectedAccount === id ? "font-semibold text-blue-600" : ""}`}
                              onMouseDown={() => handleAccountSelect(id)}
                              onMouseEnter={() => setHighlightedIndex(index)}
                            >
                              {highlightMatch(id, accountInput)}
                            </li>
                          ))}
                        </ul>
                      ) : (
                        <div className="px-4 py-3 text-sm text-slate-500 text-center">
                          No accounts found
                        </div>
                      )}
                    </div>
                  )}
                </div>
              </div>

              {/* Search Bar */}
              <div className="relative flex-1">
                <Search className="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-slate-400 z-10" />
                <input
                  ref={searchInputRef}
                  className="w-full pl-12 pr-4 py-2.5 rounded-lg bg-slate-50 border-2 border-slate-200 focus:border-blue-500 focus:bg-white transition-all outline-none text-slate-900 placeholder-slate-400"
                  placeholder="Search by VIN, vehicle, buyer, or ID... (typo tolerant)"
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
                          className={`px-4 py-2.5 cursor-pointer transition-colors ${
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
        <div className="max-w-7xl mx-auto px-6">
          <div className="flex gap-1 overflow-x-auto">
            {tabs.map((tab) => {
              const Icon = tab.icon;
              const isActive = activeTab === tab.id;
              return (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`flex items-center gap-2 px-6 py-4 font-medium transition-all border-b-2 whitespace-nowrap ${
                    isActive
                      ? "text-blue-600 border-blue-600"
                      : "text-slate-600 border-transparent hover:text-slate-900 hover:bg-slate-50"
                  }`}
                >
                  <Icon className="w-4 h-4" />
                  {tab.label}
                </button>
              );
            })}
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="max-w-7xl mx-auto px-6 py-8">
        <div className="space-y-8">
          {showSection("offers") && (
            <Section
              title="Offers"
              icon={Package}
              items={filteredResults.offers}
              render={(o: Offer) => (
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-2">
                      <span className="text-lg font-bold text-slate-900">
                        {o.year} {o.make} {o.model}
                      </span>
                      <span className="px-2 py-1 bg-emerald-100 text-emerald-700 text-xs font-semibold rounded-full">
                        {o.price}
                      </span>
                    </div>
                    <div className="text-sm text-slate-600 space-y-1">
                      <div>Offer ID: <span className="font-mono text-slate-900">{o.id}</span></div>
                      <div>VIN: <span className="font-mono text-slate-900">{o.vin}</span></div>
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
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-2">
                      <span className="text-lg font-bold text-slate-900">{p.id}</span>
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
                    <div className="text-sm text-slate-600 space-y-1">
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
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-2">
                      <span className="text-lg font-bold text-slate-900">{t.id}</span>
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
                    <div className="text-sm text-slate-600 space-y-1">
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
              <div className="text-center py-16">
                <div className="inline-flex items-center justify-center w-16 h-16 bg-slate-100 rounded-full mb-4">
                  <Search className="w-8 h-8 text-slate-400" />
                </div>
                <h3 className="text-xl font-semibold text-slate-900 mb-2">No results found</h3>
                <p className="text-slate-600">Try adjusting your search terms</p>
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
      <div className="flex items-center gap-2 mb-4">
        <Icon className="w-5 h-5 text-slate-600" />
        <h2 className="text-xl font-bold text-slate-900">{title}</h2>
        <span className="px-2 py-0.5 bg-slate-200 text-slate-700 text-xs font-semibold rounded-full">
          {items.length}
        </span>
      </div>
      <div className="grid gap-4">
        {items.map((item, idx) => (
          <div
            key={idx}
            className="p-6 rounded-xl bg-white border border-slate-200 hover:border-slate-300 hover:shadow-md transition-all"
          >
            {render(item)}
          </div>
        ))}
      </div>
    </div>
  );
}
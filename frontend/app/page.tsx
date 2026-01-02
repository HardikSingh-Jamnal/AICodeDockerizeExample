"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import { searchDocuments, SearchDocument } from "@/lib/mockSearch";

export default function Home() {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<SearchDocument[]>([]);
  const [loading, setLoading] = useState(false);
  const [hasSearched, setHasSearched] = useState(false);

  const [userType, setUserType] = useState("customer");
  const [accountId, setAccountId] = useState("ACC-001");

  // Debounced search effect
  useEffect(() => {
    let isActive = true;

    // Clear results if query is empty
    if (!query.trim()) {
      setResults([]);
      setHasSearched(false);
      setLoading(false);
      return;
    }

    setLoading(true);

    const debounceTimer = setTimeout(async () => {
      try {
        const data = await searchDocuments(query);
        if (isActive) {
          setResults(data);
          setHasSearched(true);
        }
      } catch (error) {
        console.error("Search failed", error);
      } finally {
        if (isActive) {
          setLoading(false);
        }
      }
    }, 300); // 300ms debounce as per Architecture Guidelines

    return () => {
      isActive = false;
      clearTimeout(debounceTimer);
    };
  }, [query]);

  // Currency formatter
  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(price);
  };

  return (
    <div className="search-wrapper">
      <h1 className="hero-title">
        <span className="text-gradient">Uni-Search</span> Platform
      </h1>

      <div className="filter-container">
        <select
          value={userType}
          onChange={(e) => setUserType(e.target.value)}
          className="filter-select glass"
        >
          <option value="customer">Customer</option>
          <option value="partner">Partner</option>
          <option value="employee">Employee</option>
          <option value="admin">Admin</option>
        </select>

        <select
          value={accountId}
          onChange={(e) => setAccountId(e.target.value)}
          className="filter-select glass"
        >
          <option value="ACC-001">ACC-001 (US)</option>
          <option value="ACC-002">ACC-002 (EU)</option>
          <option value="ACC-003">ACC-003 (APAC)</option>
          <option value="Global">Global Account</option>
        </select>
      </div>

      <div className="search-input-container">
        <div className="search-input-wrapper">
          {/* Search Icon (SVG) */}
          <svg
            className="search-icon"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <circle cx="11" cy="11" r="8"></circle>
            <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
          </svg>

          <input
            type="text"
            className="search-input"
            placeholder="Search products, services, offers..."
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            spellCheck={false}
            autoFocus
          />
        </div>
      </div>

      {loading && (
        <div className="loading-spinner"></div>
      )}

      {!loading && (
        <div className="results-grid">
          {results.map((doc) => (
            <Link key={doc.id} href={`/products/${doc.id}`}>
              <div className="result-card glass">
                <span className="card-category">{doc.category}</span>

                {/* Title with highlighting logic */}
                <h3
                  className="card-title highlight"
                  dangerouslySetInnerHTML={{
                    __html: doc.highlight?.title || doc.title
                  }}
                />

                <p className="card-description">
                  {doc.description.length > 80
                    ? doc.description.substring(0, 80) + "..."
                    : doc.description}
                </p>

                <div className="card-price">
                  {formatPrice(doc.price)}
                </div>
              </div>
            </Link>
          ))}
        </div>
      )}

      {!loading && hasSearched && results.length === 0 && (
        <div className="empty-state glass">
          <h3>No results found</h3>
          <p>Try searching for "Tesla", "Pump", or "Service"</p>
        </div>
      )}

      {!loading && !hasSearched && query.trim() === "" && (
        <div className="empty-state">
          <p>Start typing to search across the enterprise...</p>
        </div>
      )}
    </div>
  );
}

export interface SearchDocument {
    id: string;
    title: string;
    description: string;
    category: string;
    price: number;
    highlight?: {
        title?: string;
    };
}

const MOCK_DATA: SearchDocument[] = [
    {
        id: "1",
        title: "Tesla Model 3 Long Range",
        description: "Electric vehicle with 358 miles of range, dual motor all-wheel drive, and autopilot capabilities.",
        category: "Vehicles",
        price: 45990
    },
    {
        id: "2",
        title: "iPhone 15 Pro Max",
        description: "Titanium design, A17 Pro chip, 48MP Main camera, and USB-C support. The ultimate smartphone experience.",
        category: "Electronics",
        price: 1199
    },
    {
        id: "3",
        title: "Industrial Hydraulic Pump Station",
        description: "High-pressure hydraulic power unit for heavy industrial applications. 3000 PSI max pressure.",
        category: "Industrial",
        price: 12500
    },
    {
        id: "4",
        title: "Logistics Transport Container 40ft",
        description: "Standard ISO shipping container, weather-resistant steel construction, suitable for global freight.",
        category: "Logistics",
        price: 3500
    },
    {
        id: "5",
        title: "Downtown Office Space Lease",
        description: "Premium office suite in the financial district. 2000 sq ft, panoramic views, fully furnished.",
        category: "Real Estate",
        price: 5000
    },
    {
        id: "6",
        title: "Enterprise Cloud Server Instance",
        description: "Dedicated bare-metal server for high-performance computing. 64-core CPU, 512GB RAM.",
        category: "Services",
        price: 899
    },
    {
        id: "7",
        title: "Sony WH-1000XM5 Headphones",
        description: "Industry-leading noise canceling wireless headphones with 30-hour battery life and crystal clear call quality.",
        category: "Electronics",
        price: 348
    },
    {
        id: "8",
        title: "Solar Panel Installation Service",
        description: "Complete residential solar power system design and installation. Includes grid-tie inverter and monitoring.",
        category: "Services",
        price: 15000
    }
];

export async function searchDocuments(query: string): Promise<SearchDocument[]> {
    // Simulate network latency (300-600ms)
    await new Promise(resolve => setTimeout(resolve, 400));

    if (!query || query.trim() === "") {
        return [];
    }

    const normalizedQuery = query.toLowerCase().trim();
    // Safe regex for highlighting (escaping special chars)
    const safeQuery = query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const highlightRegex = new RegExp(`(${safeQuery})`, 'gi');

    const results = MOCK_DATA.filter(doc => {
        return (
            doc.title.toLowerCase().includes(normalizedQuery) ||
            doc.description.toLowerCase().includes(normalizedQuery) ||
            doc.category.toLowerCase().includes(normalizedQuery)
        );
    }).map(doc => {
        // Add highlighting
        const highlightedTitle = doc.title.replace(highlightRegex, '<em>$1</em>');

        return {
            ...doc,
            highlight: {
                title: highlightedTitle
            }
        };
    });

    return results;
}

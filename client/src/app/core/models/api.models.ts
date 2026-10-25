/**
 * TypeScript interfaces that mirror the backend ViewModels (Shop.DAL.ViewModels)
 * and the ApiResponse<T> envelope (Shop.BLL.Response_VM). Keeping these aligned
 * with the server contract gives the Angular services strong typing end-to-end.
 */

/** Standard response envelope returned by every API endpoint. */
export interface ApiResponse<T> {
  success: boolean;
  message?: string | null;
  statusCode: number;
  data: T;
}

/** A page of results plus the total count, used to preserve browse position. */
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/** Compact product row for catalog listings. */
export interface ProductListItem {
  id: number;
  name: string;
  slug: string;
  brand?: string | null;
  basePrice: number;
  categoryId: number;
  categoryName?: string | null;
  isActive: boolean;
  primaryImageUrl?: string | null;
  hasVariants: boolean;
  showStockQuantity: boolean;
  stockQuantity: number;
  lowStockThreshold: number;
}

/** Full product detail for the product page. */
export interface ProductDetail {
  id: number;
  name: string;
  slug: string;
  descriptionHtml?: string | null;
  brand?: string | null;
  basePrice: number;
  categoryId: number;
  categoryName?: string | null;
  isActive: boolean;
  variants: ProductVariant[];
  images: ProductImage[];
  hasVariants: boolean;
  showStockQuantity: boolean;
  stockQuantity: number;
  lowStockThreshold: number;
}

/** A purchasable size/color variant of a product (normalized shape). */
export interface ProductVariant {
  id: number;
  sizeId?: number | null;
  sizeName?: string | null;
  colorId?: number | null;
  colorName?: string | null;
  sku?: string | null;
  price: number;
  stockQuantity: number;
  isActive: boolean;
  imageUrl?: string | null;
}

/** An admin-managed size dimension. */
export interface Size {
  id: number;
  name: string;
  displayOrder: number;
  isActive: boolean;
}

/** An admin-managed color dimension. */
export interface Color {
  id: number;
  name: string;
  hexCode?: string | null;
  displayOrder: number;
  isActive: boolean;
}

export interface ProductImage {
  id: number;
  url: string;
  altText?: string | null;
  sortOrder: number;
}

export interface Category {
  id: number;
  name: string;
  slug: string;
  description?: string | null;
  imageUrl?: string | null;
  parentCategoryId?: number | null;
  children: Category[];
}

/** A requested cart/checkout line (price is always resolved server-side). */
export interface CartLineRequest {
  productId: number;
  variantId?: number | null;
  quantity: number;
}

/** A server-priced cart. */
export interface Cart {
  lines: CartLine[];
  subtotal: number;
}

export interface CartLine {
  productId: number;
  variantId?: number | null;
  productName: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

/** Guest checkout request payload. */
export interface PlaceOrderRequest {
  guestName: string;
  guestEmail: string;
  lines: CartLineRequest[];
  discountCode?: string | null;
  addressLine1: string;
  addressLine2?: string | null;
  city: string;
  stateRegion: string;
  postalCode: string;
  country: string;
  contactPhone: string;
  paymentMethod: 'Cash' | 'Card';
}

/** Order confirmation returned after a successful checkout. */
export interface OrderConfirmation {
  orderNumber: string;
  total: number;
  status: string;
  placedAt: string;
  currencyCode: string;
  paymentMethod: string;
  paymentStatus: string;
  addressLine1: string;
  addressLine2?: string | null;
  city: string;
  stateRegion: string;
  postalCode: string;
  country: string;
  contactPhone: string;
}

/** Order tracking view returned for a given order number. */
export interface OrderTracking {
  orderNumber: string;
  status: string;
  placedAt: string;
  items: OrderItem[];
  total: number;
}

export interface OrderItem {
  productNameSnapshot: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

/** Store-wide currency settings (mirrors StoreSettingsVM). */
export interface StoreSettings {
  currencyCode: string;
  currencySymbol: string;
}

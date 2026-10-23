/**
 * Models for customer accounts and reviews, mirroring the backend ViewModels
 * (CustomerAuthViewModels, ReviewViewModels).
 */

/** Customer registration request (local account). */
export interface CustomerRegisterRequest {
  fullName: string;
  email: string;
  password: string;
  phone?: string | null;
  marketingConsent: boolean;
}

/** Customer login request (local account). */
export interface CustomerLoginRequest {
  email: string;
  password: string;
}

/** Social login request (Google / Facebook). */
export interface SocialLoginRequest {
  provider: 'Google' | 'Facebook';
  credential: string;
}

/** Customer auth response carrying the JWT + profile summary. */
export interface CustomerAuthResponse {
  token: string;
  expires: string;
  customerId: number;
  fullName: string;
  email: string;
}

/** Customer profile view / update payload. */
export interface CustomerProfile {
  id: number;
  fullName: string;
  email: string;
  phone?: string | null;
  marketingConsent: boolean;
}

/** Create-review request (customer taken from token server-side). */
export interface CreateReviewRequest {
  productId: number;
  orderId?: number | null;
  rating: number;
  comment?: string | null;
}

/** A single review as shown on a product page or in the account area. */
export interface Review {
  id: number;
  productId: number;
  productName: string;
  authorName: string;
  rating: number;
  comment?: string | null;
  createdAt: string;
}

/** Aggregate review summary for a product. */
export interface ProductReviewSummary {
  productId: number;
  averageRating: number;
  reviewCount: number;
  reviews: Review[];
}

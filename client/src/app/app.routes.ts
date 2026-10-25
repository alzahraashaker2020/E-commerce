import { Routes } from '@angular/router';
import { adminGuard } from './core/guards/admin.guard';
import { accountGuard } from './core/guards/account.guard';

/**
 * Application routes. Customer-facing storefront routes are public; the admin
 * area is lazily loaded and protected by the adminGuard. Standalone components
 * are loaded with `loadComponent` to keep the initial bundle small.
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'catalog' },

  // --- Storefront (public) ---
  {
    path: 'catalog',
    loadComponent: () => import('./features/catalog/catalog.component').then(m => m.CatalogComponent)
  },
  {
    path: 'category/:slug',
    loadComponent: () => import('./features/category/category.component').then(m => m.CategoryComponent)
  },
  {
    path: 'product/:id',
    loadComponent: () => import('./features/product-detail/product-detail.component').then(m => m.ProductDetailComponent)
  },
  {
    path: 'cart',
    loadComponent: () => import('./features/cart/cart.component').then(m => m.CartComponent)
  },
  {
    path: 'checkout',
    loadComponent: () => import('./features/checkout/checkout.component').then(m => m.CheckoutComponent)
  },
  {
    path: 'confirmation/:orderNumber',
    loadComponent: () => import('./features/confirmation/confirmation.component').then(m => m.ConfirmationComponent)
  },
  {
    path: 'track',
    loadComponent: () => import('./features/tracking/tracking.component').then(m => m.TrackingComponent)
  },

  // --- Customer accounts (public auth pages + guarded account area) ---
  {
    path: 'login',
    loadComponent: () => import('./features/account/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./features/account/register/register.component').then(m => m.RegisterComponent)
  },
  {
    path: 'account',
    canActivate: [accountGuard],
    loadComponent: () => import('./features/account/account-shell/account-shell.component').then(m => m.AccountShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'profile' },
      {
        path: 'profile',
        loadComponent: () => import('./features/account/profile/profile.component').then(m => m.ProfileComponent)
      },
      {
        path: 'orders',
        loadComponent: () => import('./features/account/orders/account-orders.component').then(m => m.AccountOrdersComponent)
      },
      {
        path: 'reviews',
        loadComponent: () => import('./features/account/reviews/my-reviews.component').then(m => m.MyReviewsComponent)
      }
    ]
  },

  // --- Admin (JWT-protected, lazy) ---
  {
    path: 'admin/login',
    loadComponent: () => import('./features/admin/admin-login/admin-login.component').then(m => m.AdminLoginComponent)
  },
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadComponent: () => import('./features/admin/admin-shell/admin-shell.component').then(m => m.AdminShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/admin/admin-dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent)
      },
      {
        path: 'products',
        loadComponent: () => import('./features/admin/admin-products/admin-products.component').then(m => m.AdminProductsComponent)
      },
      {
        path: 'categories',
        loadComponent: () => import('./features/admin/admin-categories/admin-categories.component').then(m => m.AdminCategoriesComponent)
      },
      {
        path: 'sizes',
        loadComponent: () => import('./features/admin/admin-sizes/admin-sizes.component').then(m => m.AdminSizesComponent)
      },
      {
        path: 'colors',
        loadComponent: () => import('./features/admin/admin-colors/admin-colors.component').then(m => m.AdminColorsComponent)
      },
      {
        path: 'inventory',
        loadComponent: () => import('./features/admin/admin-inventory/admin-inventory.component').then(m => m.AdminInventoryComponent)
      },
      {
        path: 'orders',
        loadComponent: () => import('./features/admin/admin-orders/admin-orders.component').then(m => m.AdminOrdersComponent)
      },
      {
        path: 'discounts',
        loadComponent: () => import('./features/admin/admin-discounts/admin-discounts.component').then(m => m.AdminDiscountsComponent)
      },
      {
        path: 'customers',
        loadComponent: () => import('./features/admin/admin-customers/admin-customers.component').then(m => m.AdminCustomersComponent)
      }
    ]
  },

  { path: '**', redirectTo: 'catalog' }
];

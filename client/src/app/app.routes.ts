import { saasGuard, standaloneGuard, commerceGuard, customerStoreGuard, featureGuard, merchantLoginHostGuard } from './core/guards/capability.guard';
import { Routes } from '@angular/router';
import { merchantGuard } from './core/guards/merchant.guard';
import { accountGuard } from './core/guards/account.guard';
import { platformGuard } from './core/guards/platform.guard';

/**
 * Application routes. Customer-facing storefront routes are public; the merchant
 * area is lazily loaded and protected by the merchantGuard. Standalone components
 * are loaded with `loadComponent` to keep the initial bundle small.
 */
export const routes: Routes = [
  {path:'platform/home',canActivate:[saasGuard],loadComponent:()=>import('./features/platform/platform-home.component').then(m=>m.PlatformHomeComponent)},
  {path:'platform/shop',canActivate:[saasGuard],loadComponent:()=>import('./features/platform/platform-marketplace.component').then(m=>m.PlatformMarketplaceComponent)},
  {path:'platform/website',canActivate:[saasGuard,platformGuard],loadComponent:()=>import('./features/platform/platform-website.component').then(m=>m.PlatformWebsiteComponent)},
  {path:'platform/applications',canActivate:[saasGuard,platformGuard],loadComponent:()=>import('./features/platform/merchant-applications.component').then(m=>m.MerchantApplicationsComponent)},
  {path:'platform/operations',canActivate:[saasGuard,platformGuard],loadComponent:()=>import('./features/platform/platform-operations.component').then(m=>m.PlatformOperationsComponent)},
  ...['forgot-password','reset-password','verify-email'].map(path => ({path:'account/'+path,loadComponent:()=>import('./features/account/account-recovery.component').then(m=>m.AccountRecoveryComponent)})),
  { path: 'platform/plans', canActivate: [saasGuard,platformGuard], loadComponent: () => import('./features/platform/platform-plans.component').then(m => m.PlatformPlansComponent) },
  { path: 'platform', canActivate:[saasGuard], loadComponent: () => import('./features/platform/platform.component').then(m => m.PlatformComponent) },
  { path: 'platform/login', redirectTo: 'platform', pathMatch: 'full' },
  { path: 'admin', redirectTo: 'merchant', pathMatch: 'full' },
  { path: 'admin/:page', redirectTo: 'merchant/:page', pathMatch: 'full' },
  { path: '', pathMatch: 'full', loadComponent:()=>import('./features/platform/platform-entry.component').then(m=>m.PlatformEntryComponent) },

  // --- Storefront (public) ---
  {
    path: 'catalog',canActivate:[customerStoreGuard],
    loadComponent: () => import('./features/kids/storefront-page.component').then(m => m.StorefrontPageComponent)
  },
  {
    path: 'category/:slug',
    loadComponent: () => import('./features/category/category.component').then(m => m.CategoryComponent)
  },
  {
    path: 'product/:id',canActivate:[customerStoreGuard],
    loadComponent: () => import('./features/product-detail/product-detail.component').then(m => m.ProductDetailComponent)
  },
  {
    path: 'cart', canActivate:[commerceGuard],
    loadComponent: () => import('./features/cart/cart.component').then(m => m.CartComponent)
  },
  {
    path: 'checkout', canActivate:[commerceGuard],
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
    path: 'login',canActivate:[customerStoreGuard],
    loadComponent: () => import('./features/account/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./features/account/registration-choice.component').then(m => m.RegistrationChoiceComponent)
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

  { path: 'register/customer', canActivate:[customerStoreGuard], loadComponent: () => import('./features/account/register/register.component').then(m => m.RegisterComponent) },
  { path: 'merchant/register', loadComponent: () => import('./features/account/merchant-registration.component').then(m => m.MerchantRegistrationComponent) },

  // --- Merchant (JWT-protected, lazy) ---
  {
    path: 'merchant/login',canActivate:[merchantLoginHostGuard],
    loadComponent: () => import('./features/merchant/merchant-login/merchant-login.component').then(m => m.MerchantLoginComponent)
  },
  {
    path: 'merchant',
    canActivate: [merchantGuard],
    loadComponent: () => import('./features/merchant/merchant-shell/merchant-shell.component').then(m => m.MerchantShellComponent),
    children: [
      {path:'operations',canActivate:[standaloneGuard],loadComponent:()=>import('./features/platform/platform-operations.component').then(m=>m.PlatformOperationsComponent)},
      {path:'notifications',loadComponent:()=>import('./features/merchant/merchant-notifications.component').then(m=>m.MerchantNotificationsComponent)},
        {path:'returns',loadComponent:()=>import('./features/merchant/merchant-returns.component').then(m=>m.MerchantReturnsComponent)},
        {path:'activity',loadComponent:()=>import('./features/merchant/merchant-activity.component').then(m=>m.MerchantActivityComponent)},
        {path:'languages',canActivate:[featureGuard],data:{feature:'StorefrontDesign'},loadComponent:()=>import('./features/merchant/merchant-languages.component').then(m=>m.MerchantLanguagesComponent)},
        {path:'storefront',canActivate:[featureGuard],data:{feature:'StorefrontDesign'},loadComponent:()=>import('./features/merchant/merchant-storefront.component').then(m=>m.MerchantStorefrontComponent)},
      { path: 'subscription', canActivate:[saasGuard], loadComponent: () => import('./features/merchant/merchant-subscription.component').then(m => m.MerchantSubscriptionComponent) },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'delivery', loadComponent: () => import('./features/merchant/merchant-delivery/merchant-delivery.component').then(m => m.MerchantDeliveryComponent) },
      { path: 'currencies', data: { kind: 'currencies' }, loadComponent: () => import('./features/merchant/merchant-configuration/merchant-configuration.component').then(m => m.MerchantConfigurationComponent) },
      { path: 'order-statuses', data: { kind: 'statuses' }, loadComponent: () => import('./features/merchant/merchant-configuration/merchant-configuration.component').then(m => m.MerchantConfigurationComponent) },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/merchant/merchant-dashboard/merchant-dashboard.component').then(m => m.MerchantDashboardComponent)
      },
      {
        path: 'products',
        loadComponent: () => import('./features/merchant/merchant-products/merchant-products.component').then(m => m.MerchantProductsComponent)
      },
      {
        path: 'categories',
        loadComponent: () => import('./features/merchant/merchant-categories/merchant-categories.component').then(m => m.MerchantCategoriesComponent)
      },
      {
        path: 'sizes',
        loadComponent: () => import('./features/merchant/merchant-sizes/merchant-sizes.component').then(m => m.MerchantSizesComponent)
      },
      {
        path: 'colors',
        loadComponent: () => import('./features/merchant/merchant-colors/merchant-colors.component').then(m => m.MerchantColorsComponent)
      },
      {
        path: 'inventory',
        loadComponent: () => import('./features/merchant/merchant-inventory/merchant-inventory.component').then(m => m.MerchantInventoryComponent)
      },
      {
        path: 'orders',
        loadComponent: () => import('./features/merchant/merchant-orders/merchant-orders.component').then(m => m.MerchantOrdersComponent)
      },
      {
        path: 'discounts',canActivate:[featureGuard],data:{feature:'Discounts'},
        loadComponent: () => import('./features/merchant/merchant-discounts/merchant-discounts.component').then(m => m.MerchantDiscountsComponent)
      },
      {
        path: 'customers',
        loadComponent: () => import('./features/merchant/merchant-customers/merchant-customers.component').then(m => m.MerchantCustomersComponent)
      }
    ]
  },

  { path: '**', redirectTo: 'catalog' }
];

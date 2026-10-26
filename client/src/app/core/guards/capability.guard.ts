import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { RuntimeService } from '../services/runtime.service';

export const saasGuard:CanActivateFn=()=>inject(RuntimeService).saasEnabled||inject(Router).createUrlTree(['/']);
export const standaloneGuard:CanActivateFn=()=>inject(RuntimeService).operationsEnabled||inject(Router).createUrlTree(['/merchant']);
export const commerceGuard:CanActivateFn=async()=>{
  const runtime=inject(RuntimeService),router=inject(Router);
  try{await runtime.refreshStore();}catch{return router.createUrlTree(['/catalog']);}
  return runtime.commerceEnabled||router.createUrlTree(['/catalog']);
};

export const customerStoreGuard:CanActivateFn=()=>!inject(RuntimeService).isPlatformHost||inject(Router).createUrlTree(['/platform/shop']);

export const featureGuard:CanActivateFn=async(route)=>{const runtime=inject(RuntimeService),router=inject(Router);try{await runtime.refreshStore();}catch{return router.createUrlTree(['/merchant/login']);}return runtime.hasFeature(route.data['feature'])||router.createUrlTree(['/merchant/subscription']);};
export const merchantLoginHostGuard:CanActivateFn=()=>!inject(RuntimeService).isPlatformHost||inject(Router).createUrlTree(['/merchant/register']);

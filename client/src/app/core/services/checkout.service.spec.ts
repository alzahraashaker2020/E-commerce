import { of, throwError } from 'rxjs';
import { HttpHeaders } from '@angular/common/http';
import { CheckoutService } from './checkout.service';
describe('Durable checkout recovery',()=>{
 let api:any,service:CheckoutService;
 const request:any={guestName:'Test',lines:[{productId:1,quantity:1}]};
 const key='shop_checkout_pending:'+location.hostname+':guest';
 beforeEach(()=>{localStorage.removeItem(key);api={postEnvelope:jasmine.createSpy('postEnvelope')};service=new CheckoutService(api,{getToken:()=>null} as any);});
 afterEach(()=>{localStorage.removeItem(key);localStorage.removeItem('shop_tracking_access:'+location.hostname+':ORDER-TEST');});
 it('retains identical key on unknown outcome and blocks changed details',()=>{api.postEnvelope.and.returnValue(throwError(()=>({status:0})));service.placeOrder(request).subscribe({error:()=>{}});const pending=service.pendingRequest();expect(pending?.idempotencyKey).toBeTruthy();service.placeOrder(request).subscribe({error:()=>{}});expect(api.postEnvelope.calls.mostRecent().args[1].idempotencyKey).toBe(pending!.idempotencyKey);service.placeOrder({...request,guestName:'Changed'}).subscribe({error:()=>{}});expect(api.postEnvelope.calls.count()).toBe(2);});
 for(const error of [{status:400},{status:409,headers:new HttpHeaders({'X-Checkout-Outcome':'NotCreated'})}])it('clears pending only when no order was created '+JSON.stringify(error),()=>{api.postEnvelope.and.returnValue(throwError(()=>error));service.placeOrder(request).subscribe({error:()=>{}});expect(service.pendingRequest()).toBeNull();});
 it('clears on confirmation and preserves tracking proof',()=>{api.postEnvelope.and.returnValue(of({success:true,data:{orderNumber:'ORDER-TEST',trackingToken:'private-proof'}}));service.placeOrder(request).subscribe();expect(service.pendingRequest()).toBeNull();expect(localStorage.getItem('shop_tracking_access:'+location.hostname+':ORDER-TEST')).toBe('private-proof');});
});

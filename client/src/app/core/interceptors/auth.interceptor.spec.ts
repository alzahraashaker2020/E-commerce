import { TestBed } from '@angular/core/testing';
import { provideHttpClient,withInterceptors,HttpClient } from '@angular/common/http';
import { provideHttpClientTesting,HttpTestingController } from '@angular/common/http/testing';
import { authInterceptor } from './auth.interceptor';
import { MerchantAuthService } from '../services/merchant-auth.service';
import { AccountService } from '../services/account.service';
import { environment } from '../../../environments/environment';
describe('Actor-specific JWT routing',()=>{
 let http:HttpClient,backend:HttpTestingController;let old:string|null;
 beforeEach(()=>{old=localStorage.getItem('shop_platform_token');localStorage.setItem('shop_platform_token','platform-token');TestBed.configureTestingModule({providers:[provideHttpClient(withInterceptors([authInterceptor])),provideHttpClientTesting(),{provide:MerchantAuthService,useValue:{getToken:()=> 'merchant-token'}},{provide:AccountService,useValue:{getToken:()=> 'customer-token'}}]});http=TestBed.inject(HttpClient);backend=TestBed.inject(HttpTestingController);});
 afterEach(()=>{backend.verify();if(old===null)localStorage.removeItem('shop_platform_token');else localStorage.setItem('shop_platform_token',old);});
 for(const [path,token] of [['merchant/products','merchant-token'],['account/profile','customer-token'],['checkout','customer-token'],['platform/stores','platform-token'],['catalog',null],['merchant/auth/login',null]])it('selects token for '+path,()=>{const url=environment.apiBaseUrl+'/'+path;http.get(url).subscribe();const req=backend.expectOne(url);expect(req.request.headers.get('Authorization')).toBe(token?'Bearer '+token:null);req.flush({});});
 it('never transmits token to another destination',()=>{http.get('https://example.test/merchant/products').subscribe();const req=backend.expectOne('https://example.test/merchant/products');expect(req.request.headers.has('Authorization')).toBeFalse();req.flush({});});
});

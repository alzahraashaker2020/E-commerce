import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ApiService } from './api.service';
import { environment } from '../../../environments/environment';
describe('API wire compatibility',()=>{
 let service:ApiService,http:HttpTestingController;
 beforeEach(()=>{TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting()]});service=TestBed.inject(ApiService);http=TestBed.inject(HttpTestingController);});
 afterEach(()=>http.verify());
 it('unwraps data while preserving query values',()=>{let data:any;service.get('catalog',{q:'dress',page:2,skip:null}).subscribe(x=>data=x);let req=http.expectOne(r=>r.url===environment.apiBaseUrl+'/catalog');expect(req.request.params.get('q')).toBe('dress');expect(req.request.params.has('skip')).toBeFalse();req.flush({success:true,message:null,statusCode:200,data:{items:[]}});expect(data).toEqual({items:[]});});
 it('keeps checkout messages in full envelope',()=>{let result:any;service.postEnvelope('checkout',{}).subscribe(x=>result=x);http.expectOne(environment.apiBaseUrl+'/checkout').flush({success:true,message:'Discount skipped',data:{},statusCode:200});expect(result.message).toBe('Discount skipped');});
});

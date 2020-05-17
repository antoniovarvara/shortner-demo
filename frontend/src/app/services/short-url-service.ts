import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BackendApiResponse } from '../models/apiResponse';
import { catchError, map, tap } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ShortUrlService{

  private backendCreateShortUrl = environment.baseBackendUrl + '/shortUrl';

  constructor( private http: HttpClient,) { }

  createShortUrl(userUrl: string): Observable<BackendApiResponse> {
    return this.http.post<BackendApiResponse>(this.backendCreateShortUrl, {originalUrl: userUrl})
  }



}

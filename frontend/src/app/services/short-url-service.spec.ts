import { TestBed } from '@angular/core/testing';

import { ShortUrlService } from './short-url-service';

describe('ShortUrlService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: ShortUrlService = TestBed.get(ShortUrlService);
    expect(service).toBeTruthy();
  });
});

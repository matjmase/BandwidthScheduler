import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GridRenderingGeneratedComponent } from './grid-rendering-generated.component';

describe('GridRenderingGeneratedComponent', () => {
  let component: GridRenderingGeneratedComponent;
  let fixture: ComponentFixture<GridRenderingGeneratedComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [GridRenderingGeneratedComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(GridRenderingGeneratedComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

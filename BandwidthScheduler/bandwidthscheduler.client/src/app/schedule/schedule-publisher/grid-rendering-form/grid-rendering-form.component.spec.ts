import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GridRenderingFormComponent } from './grid-rendering-form.component';

describe('GridRenderingFormComponent', () => {
  let component: GridRenderingFormComponent;
  let fixture: ComponentFixture<GridRenderingFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [GridRenderingFormComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(GridRenderingFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
